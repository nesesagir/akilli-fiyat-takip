export type UserDto = {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  displayName: string;
  isActive: boolean;
  preferredCurrency: string;
  preferredLanguage: string;
  emailNotificationsEnabled: boolean;
  createdAtUtc: string;
  trackedItemCount?: number;
};

export type RegisteredUserListItemDto = UserDto;

export type UpdateUserProfileRequest = {
  firstName: string;
  lastName: string;
  email: string;
  preferredCurrency: string;
  preferredLanguage: string;
  emailNotificationsEnabled: boolean;
};

export type TrackedItemDto = {
  id: string;
  userId: string;
  productUrl: string;
  title: string;
  imageUrl?: string | null;
  storeName?: string | null;
  currency: string;
  currentPrice?: number | null;
  targetPrice: number;
  isInStock: boolean;
  isActive: boolean;
  lastCheckedAtUtc?: string | null;
  progressToTargetPercent?: number | null;
};

export type DashboardSummaryDto = {
  potentialMonthlySavings: number;
  dealOfTheDay?: TrackedItemDto | null;
  items: TrackedItemDto[];
};

export type CreateTrackedItemRequest = {
  userId: string;
  productUrl: string;
  targetPrice: number;
  title?: string;
};

export type PriceHistoryPointDto = {
  id: string;
  price: number;
  isInStock: boolean;
  recordedAtUtc: string;
};
