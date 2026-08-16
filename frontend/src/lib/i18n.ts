export type AppLang = "tr" | "en";

export type UiCopy = {
  brand: string;
  account: string;
  personalize: string;
  close: string;
  profile: string;
  prefs: string;
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  currency: string;
  language: string;
  notifications: string;
  notificationsHint: string;
  save: string;
  saving: string;
  saved: string;
  loading: string;
  loadError: string;
  saveError: string;
  emailTaken: string;
  langTr: string;
  langEn: string;
  hi: string;
  yourTracked: string;
  yourTrackedHere: string;
  refresh: string;
  signOut: string;
  apiFailed: string;
  apiFailedHint: string;
  trackedProducts: string;
  noProducts: string;
  addProduct: string;
  addProductHint: string;
  link: string;
  targetPrice: string;
  titleOptional: string;
  titlePlaceholder: string;
  add: string;
  fetchingPrice: string;
  addFailed: string;
  monthlySavings: string;
  estimated: string;
  savingsHint: string;
  dealOfDay: string;
  noDeal: string;
  noDealHint: string;
  noImage: string;
  store: string;
  current: string;
  target: string;
  remaining: string;
  toTarget: string;
  detailsChart: string;
  goToStore: string;
  outOfStock: string;
  unknownStore: string;
  deleteProduct: string;
  delete: string;
  confirmDelete: string;
  confirmDeleteHint: string;
  confirmDeleteYes: string;
  deleting: string;
  cancel: string;
  product: string;
  last30Days: string;
  checkPrice: string;
  checking: string;
  goToProduct: string;
  removeTracking: string;
  noHistory: string;
  price: string;
  drop: string;
  rise: string;
  onboardingHeadline1: string;
  onboardingHeadline2: string;
  onboardingHint: string;
  continue: string;
  needFirstName: string;
  needLastName: string;
  needEmail: string;
  needPassword: string;
  register: string;
  login: string;
  loginFailed: string;
  haveAccount: string;
  needAccount: string;
  registerFailed: string;
  connectionFailed: string;
  emailAlreadyUsed: string;
  rightsReserved: string;
  copyright: string;
};

const tr: UiCopy = {
  brand: "Akıllı Fiyat Takip",
  account: "Hesap",
  personalize: "Kişiselleştir",
  close: "Kapat",
  profile: "Profil",
  prefs: "Tercihler",
  firstName: "Ad",
  lastName: "Soyad",
    email: "E-posta",
    password: "Şifre",
    currency: "Para birimi",
  language: "Dil",
  notifications: "E-posta bildirimleri",
  notificationsHint: "Hedef fiyata düşünce haber ver.",
  save: "Kaydet",
  saving: "Kaydediliyor…",
  saved: "Kaydedildi.",
  loading: "Yükleniyor…",
  loadError: "Yüklenemedi",
  saveError: "Kaydedilemedi",
  emailTaken: "Bu e-posta zaten kullanılıyor.",
  langTr: "Türkçe",
  langEn: "English",
  hi: "Merhaba",
  yourTracked: "Takip ettiklerin",
  yourTrackedHere: "Takip ettiklerin burada.",
  refresh: "Yenile",
  signOut: "Çıkış",
  apiFailed: "API’ye bağlanılamadı",
  apiFailedHint: "Backend’in http://localhost:5080 adresinde çalıştığından emin ol.",
  trackedProducts: "Takip edilen ürünler",
  noProducts: "Henüz ürün yok. Yukarıdan ilk ürün linkini ekle.",
  addProduct: "Ürün ekle",
  addProductHint: "Takip etmek istediğin ürünün linkini yapıştır.",
  link: "Link",
  targetPrice: "Hedef fiyat",
  titleOptional: "Başlık",
  titlePlaceholder: "İsteğe bağlı",
  add: "Ekle",
  fetchingPrice: "Görsel ve fiyat alınıyor…",
  addFailed: "Eklenemedi",
  monthlySavings: "Bu ayki tasarruf",
  estimated: "Tahmini",
  savingsHint: "Hedefine inebilecek ürünlerden toplam fark. Fiyat düşünce burası büyür.",
  dealOfDay: "Günün fırsatı",
  noDeal: "Henüz fırsat yok",
  noDealHint: "Ürün ekledikçe hedefe en yakın aday burada belirir.",
  noImage: "Görsel yok",
  store: "Mağaza",
  current: "Güncel",
  target: "Hedef",
  remaining: "Kalan",
  toTarget: "Hedefe",
  detailsChart: "Detay & grafik",
  goToStore: "Mağazaya git",
  outOfStock: "Stokta yok",
  unknownStore: "Bilinmiyor",
  deleteProduct: "Ürünü sil",
  delete: "Sil",
  confirmDelete: "Emin misin?",
  confirmDeleteHint: "Bu ürün takipten çıkarılacak.",
  confirmDeleteYes: "Eminim, sil",
  deleting: "Siliniyor…",
  cancel: "Vazgeç",
  product: "Ürün",
  last30Days: "Son 30 gün",
  checkPrice: "Fiyatı kontrol et",
  checking: "Kontrol…",
  goToProduct: "Ürün sayfasına git →",
  removeTracking: "Takibi sil",
  noHistory: "Henüz fiyat geçmişi yok. “Fiyatı kontrol et” ile ilk kaydı oluştur.",
  price: "Fiyat",
  drop: "Düşüş",
  rise: "Yükseliş",
  onboardingHeadline1: "Fiyatı takip et,",
  onboardingHeadline2: "fırsatı yakala.",
  onboardingHint:
    "Ad, soyad, e-posta ve şifrenle hesap oluştur veya giriş yap.",
  continue: "Devam et",
  needFirstName: "Adını yaz.",
  needLastName: "Soyadını yaz.",
  needEmail: "E-posta zorunlu.",
  needPassword: "Şifre en az 8 karakter olmalı.",
  register: "Kayıt ol",
  login: "Giriş yap",
  loginFailed: "E-posta veya şifre hatalı.",
  haveAccount: "Hesabın var mı? Giriş yap",
  needAccount: "Yeni misin? Kayıt ol",
  registerFailed: "Kayıt başarısız",
  connectionFailed: "Bağlantı kurulamadı. API çalışıyor mu?",
  emailAlreadyUsed: "Bu e-posta kayıtlı. Giriş yapmayı dene.",
  rightsReserved: "Tüm hakları saklıdır.",
  copyright: "Telif hakkı",
};

const en: UiCopy = {
  brand: "Akıllı Fiyat Takip",
  account: "Account",
  personalize: "Personalize",
  close: "Close",
  profile: "Profile",
  prefs: "Preferences",
  firstName: "First name",
  lastName: "Last name",
    email: "Email",
    password: "Password",
    currency: "Currency",
  language: "Language",
  notifications: "Email notifications",
  notificationsHint: "Notify me when the price hits my target.",
  save: "Save",
  saving: "Saving…",
  saved: "Saved.",
  loading: "Loading…",
  loadError: "Could not load",
  saveError: "Could not save",
  emailTaken: "This email is already in use.",
  langTr: "Türkçe",
  langEn: "English",
  hi: "Hi",
  yourTracked: "Your tracked items",
  yourTrackedHere: "Your tracked items are here.",
  refresh: "Refresh",
  signOut: "Sign out",
  apiFailed: "Could not reach the API",
  apiFailedHint: "Make sure the backend is running at http://localhost:5080.",
  trackedProducts: "Tracked products",
  noProducts: "No products yet. Paste your first product link above.",
  addProduct: "Add product",
  addProductHint: "Paste the product link you want to track.",
  link: "Link",
  targetPrice: "Target price",
  titleOptional: "Title",
  titlePlaceholder: "Optional",
  add: "Add",
  fetchingPrice: "Fetching image and price…",
  addFailed: "Could not add",
  monthlySavings: "This month’s savings",
  estimated: "Estimated",
  savingsHint:
    "Total gap from items that can still hit their target. Grows when prices drop.",
  dealOfDay: "Deal of the day",
  noDeal: "No deal yet",
  noDealHint: "As you add products, the closest to target shows up here.",
  noImage: "No image",
  store: "Store",
  current: "Current",
  target: "Target",
  remaining: "Left",
  toTarget: "To target",
  detailsChart: "Details & chart",
  goToStore: "Go to store",
  outOfStock: "Out of stock",
  unknownStore: "Unknown",
  deleteProduct: "Delete product",
  delete: "Delete",
  confirmDelete: "Are you sure?",
  confirmDeleteHint: "This product will be removed from tracking.",
  confirmDeleteYes: "Yes, delete",
  deleting: "Deleting…",
  cancel: "Cancel",
  product: "Product",
  last30Days: "Last 30 days",
  checkPrice: "Check price",
  checking: "Checking…",
  goToProduct: "Open product page →",
  removeTracking: "Remove tracking",
  noHistory: "No price history yet. Use “Check price” to create the first record.",
  price: "Price",
  drop: "Drop",
  rise: "Rise",
  onboardingHeadline1: "Track the price,",
  onboardingHeadline2: "catch the deal.",
  onboardingHint:
    "Create an account with name, email and password — or sign in.",
  continue: "Continue",
  needFirstName: "Enter your first name.",
  needLastName: "Enter your last name.",
  needEmail: "Email is required.",
  needPassword: "Password must be at least 8 characters.",
  register: "Sign up",
  login: "Sign in",
  loginFailed: "Incorrect email or password.",
  haveAccount: "Already have an account? Sign in",
  needAccount: "New here? Sign up",
  registerFailed: "Registration failed",
  connectionFailed: "Could not connect. Is the API running?",
  emailAlreadyUsed: "This email is already registered. Try signing in.",
  rightsReserved: "All rights reserved.",
  copyright: "Copyright",
};

const ui: Record<AppLang, UiCopy> = { tr, en };

/** @deprecated use uiCopy — kept for older AccountPanel imports */
export type AccountCopy = UiCopy;
export function accountCopy(lang: string | null | undefined): UiCopy {
  return uiCopy(lang);
}

export function uiCopy(lang: string | null | undefined): UiCopy {
  return lang === "en" ? ui.en : ui.tr;
}

export function normalizeLang(lang: string | null | undefined): AppLang {
  return lang === "en" ? "en" : "tr";
}

export function dateLocale(lang: AppLang): string {
  return lang === "en" ? "en-US" : "tr-TR";
}
