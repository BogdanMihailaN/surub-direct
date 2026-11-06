export interface AccountPartialReturns {
  canRequest: boolean;
  offers: { offerId: string; productName: string; sku: string; offerLabel: string; purchasedPacks: number; availablePacks: number; unitsPerPack: number }[];
  items: { id: string; status: string; createdAt: string; inventoryReceivedAt: string | null;
    lines: { offerId: string; productName: string; sku: string; offerLabel: string; packs: number; unitsPerPack: number; quantity: number }[] }[];
}
