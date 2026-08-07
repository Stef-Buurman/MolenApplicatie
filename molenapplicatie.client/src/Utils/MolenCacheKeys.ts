export class MolenCacheKeys {
  public static readonly prefix = 'molen:';
  public static readonly mapSummary = 'molen:map-summary';
  public static readonly withImageCount = 'molen:with-image-count';
  public static readonly filters = 'molen:filters';
  public static readonly detailsPrefix = 'molen:details:';
  public static readonly mapItemsPrefix = 'molen:map-items:';

  public static details(id: string): string {
    return `${this.detailsPrefix}${id}`;
  }

  public static mapItems(requestKey: string): string {
    return `${this.mapItemsPrefix}${requestKey}`;
  }
}
