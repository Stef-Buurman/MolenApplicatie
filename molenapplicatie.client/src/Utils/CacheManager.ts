import { CacheEntry } from '../Interfaces/CacheEntry';

export class CacheManager {
  private static readonly cache = new Map<string, CacheEntry<unknown>>();
  private static readonly pendingRequests = new Map<string, Promise<unknown>>();
  private static generation = 0;

  public static async getOrSet<T>(
    key: string,
    fetchFunction: () => Promise<T>,
    ttlMinutes: number,
    shouldCache?: (data: T) => boolean,
  ): Promise<T> {
    const cacheEntry = this.cache.get(key);

    if (
      cacheEntry &&
      Date.now() - cacheEntry.timestamp < ttlMinutes * 60 * 1000
    ) {
      return cacheEntry.data as T;
    }

    const pendingRequest = this.pendingRequests.get(key);
    if (pendingRequest) {
      return pendingRequest as Promise<T>;
    }

    const requestGeneration = this.generation;
    const request = fetchFunction()
      .then((freshData) => {
        if (
          requestGeneration === this.generation &&
          ttlMinutes > 0 &&
          (!shouldCache || shouldCache(freshData))
        ) {
          this.cache.set(key, {
            data: freshData,
            timestamp: Date.now(),
          });
        }

        return freshData;
      })
      .finally(() => {
        if (this.pendingRequests.get(key) === request) {
          this.pendingRequests.delete(key);
        }
      });

    this.pendingRequests.set(key, request);
    return request;
  }

  public static clear(key?: string): void {
    this.generation++;

    if (key) {
      this.cache.delete(key);
      this.pendingRequests.delete(key);
      return;
    }

    this.cache.clear();
    this.pendingRequests.clear();
  }

  public static clearByPrefix(prefix: string): void {
    this.generation++;

    for (const key of this.cache.keys()) {
      if (key.startsWith(prefix)) {
        this.cache.delete(key);
      }
    }

    for (const key of this.pendingRequests.keys()) {
      if (key.startsWith(prefix)) {
        this.pendingRequests.delete(key);
      }
    }
  }
}
