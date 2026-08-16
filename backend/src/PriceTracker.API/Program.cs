using PriceTracker.Application;
using PriceTracker.Infrastructure;
using PriceTracker.API;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Render / bulut: PORT ortam değişkeni
    var port = Environment.GetEnvironmentVariable("PORT");
    var onRender = !string.IsNullOrWhiteSpace(port);
    if (onRender)
        builder.WebHost.UseUrls($"http://+:{port}");

    // Arka plan servisi hata verse bile API ayakta kalsın.
    builder.Services.Configure<HostOptions>(options =>
    {
        options.ServicesStartConcurrently = true;
        options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
    });

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Akıllı Fiyat Takip API",
            Version = "v1",
            Description =
                "Kayıtlı kullanıcıları görmek için: Admin → Giriş (POST /api/admin/login), " +
                "ardından token ile Kullanıcılar (GET /api/admin/users)."
        });

        options.AddSecurityDefinition("AdminToken", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Description = "Yönetici token’ı. Önce Admin → Giriş yapın, response’taki token alanının TAMAMINI buraya yapıştırın (Bearer yazma).",
            Name = "X-Admin-Token",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey
        });
        options.OperationFilter<AdminTokenOperationFilter>();

        var xmlPath = Path.Combine(AppContext.BaseDirectory, "PriceTracker.API.xml");
        if (File.Exists(xmlPath))
            options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    });

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("Frontend", policy =>
            policy.WithOrigins(
                    builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
                    ?? ["http://localhost:3000"])
                .AllowAnyHeader()
                .AllowAnyMethod());
    });

    var app = builder.Build();

    if (app.Environment.IsDevelopment() || app.Configuration.GetValue("Swagger:Enabled", false))
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.DocumentTitle = "Akıllı Fiyat Takip API";
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Akıllı Fiyat Takip API v1");
        });
        Log.Information("Swagger: /swagger");
    }

    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
            var ex = feature?.Error;
            Log.Error(ex, "İşlenmeyen hata: {Path}", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                message = "Sunucu hatası",
                detail = ex?.Message
            });
        });
    });

    app.UseSerilogRequestLogging();
    app.UseCors("Frontend");
    // Render TLS'i dışarıda sonlandırır; container içinde HTTPS yönlendirme bozar.
    if (!onRender)
        app.UseHttpsRedirection();
    app.MapGet("/health", () => Results.Ok(new { status = "ok", app = "Akıllı Fiyat Takip" }));
    app.MapControllers();

    if (app.Configuration.GetValue("Database:ApplyMigrationsOnStartup", true))
    {
        try
        {
            await PriceTracker.API.DatabaseInitializer.MigrateAndSeedAsync(app.Services);
            Log.Information("Veritabanı migration'ları uygulandı.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Migration uygulanamadı. ConnectionStrings__DefaultConnection değerini kontrol edin.");
        }
    }

    if (app.Configuration.GetValue("Hangfire:Enabled", false))
    {
        try
        {
            app.UseHangfireJobs(app.Configuration);
            Log.Information("Hangfire hazır. Dashboard: /hangfire");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Hangfire başlatılamadı.");
        }
    }
    else
    {
        Log.Information("Otomatik fiyat kontrolü: PriceCheck BackgroundService");
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Uygulama başlatılamadı: {Message}", ex.Message);
    throw;
}
finally
{
    Log.CloseAndFlush();
}
