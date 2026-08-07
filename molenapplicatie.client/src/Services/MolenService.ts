import { Injectable } from '@angular/core';
import {
  BehaviorSubject,
  defer,
  firstValueFrom,
  from,
  map,
  Observable,
  Subject,
  tap,
} from 'rxjs';
import { MolenFilterList } from '../Interfaces/Filters/MolenFilterList';
import { RecentAddedImages } from '../Interfaces/MolensResponseType';
import {
  molenDeleteMolenImage,
  molenGetMapSummary,
  molenGetMolensWithImageCount,
  molenGetMolenDataById,
  molenGetMolenFilters,
  molenGetNewAddedMolens,
  molenUpdateOldestMolens,
  molenUploadImage,
} from '../api/methods/Molen.api';
import { CacheManager } from '../Utils/CacheManager';
import { MolenCacheKeys } from '../Utils/MolenCacheKeys';
import { fromTypedApi } from '../Utils/TypedApiObservable';
import { MolenData } from '../api/generated/data-contracts';

export interface MolenMapSummary {
  totalMolensWithImage: number;
  recentAddedImages: RecentAddedImages[];
}

@Injectable({
  providedIn: 'root',
})
export class MolenService {
  public selectedMolen?: MolenData;

  private readonly mapSummaryCacheTtlMinutes = 2;
  private readonly withImageCountCacheTtlMinutes = 5;
  private readonly filterCacheTtlMinutes = 15;
  private readonly molenDetailsCacheTtlMinutes = 5;

  private readonly mapSummarySubject = new BehaviorSubject<MolenMapSummary>({
    totalMolensWithImage: 0,
    recentAddedImages: [],
  });
  public readonly mapSummary$ = this.mapSummarySubject.asObservable();

  private readonly molensWithImageCountSubject = new BehaviorSubject<number>(0);
  public readonly molensWithImageCount$ =
    this.molensWithImageCountSubject.asObservable();

  private readonly mapRefreshSubject = new Subject<void>();
  public readonly mapRefresh$ = this.mapRefreshSubject.asObservable();

  public getMolenById(id: string): Observable<MolenData> {
    return defer(() =>
      from(
        CacheManager.getOrSet(
          MolenCacheKeys.details(id),
          () =>
            firstValueFrom(
              fromTypedApi(molenGetMolenDataById({ id })).pipe(
                map((molen) => molen as MolenData),
              ),
            ),
          this.molenDetailsCacheTtlMinutes,
        ),
      ),
    ).pipe(
      tap((molen) => {
        this.selectedMolen = molen;
      }),
    );
  }

  public getAllMolenFilters(): Observable<MolenFilterList> {
    return defer(() =>
      from(
        CacheManager.getOrSet(
          MolenCacheKeys.filters,
          () =>
            firstValueFrom(
              fromTypedApi(molenGetMolenFilters()).pipe(
                map((filters) => filters as MolenFilterList),
              ),
            ),
          this.filterCacheTtlMinutes,
        ),
      ),
    );
  }

  public getMapSummary(): Observable<MolenMapSummary> {
    return defer(() =>
      from(
        CacheManager.getOrSet(
          MolenCacheKeys.mapSummary,
          () =>
            firstValueFrom(
              fromTypedApi(molenGetMapSummary()).pipe(
                map((summary) => summary as MolenMapSummary),
              ),
            ),
          this.mapSummaryCacheTtlMinutes,
        ),
      ),
    ).pipe(
      tap((summary) => {
        this.mapSummarySubject.next(summary);
      }),
    );
  }

  public getMolensWithImageCount(): Observable<number> {
    return defer(() =>
      from(
        CacheManager.getOrSet(
          MolenCacheKeys.withImageCount,
          () =>
            firstValueFrom(
              fromTypedApi(molenGetMolensWithImageCount()).pipe(
                map((count) => Number(count)),
              ),
            ),
          this.withImageCountCacheTtlMinutes,
        ),
      ),
    ).pipe(
      tap((count) => {
        this.molensWithImageCountSubject.next(count);
      }),
    );
  }

  public removeSelectedMolen(): void {
    this.selectedMolen = undefined;
  }

  public updateOldestMolens(apiKey: string): Observable<MolenData[]> {
    return fromTypedApi(
      molenUpdateOldestMolens({
        params: {
          headers: {
            Authorization: apiKey,
          },
        },
      }),
    ).pipe(
      map((molens) => molens as MolenData[]),
      tap(() => this.onMolenDataChanged()),
    );
  }

  public searchForNewMolens(apiKey: string): Observable<MolenData[]> {
    return fromTypedApi(
      molenGetNewAddedMolens({
        params: {
          headers: {
            Authorization: apiKey,
          },
        },
      }),
    ).pipe(
      map((molens) => molens as MolenData[]),
      tap(() => this.onMolenDataChanged()),
    );
  }

  public deleteImage(
    tbNumber: string,
    imageName: string,
    apiKey: string,
  ): Observable<MolenData> {
    return fromTypedApi(
      molenDeleteMolenImage(
        { tbNumber, imageName },
        {
          params: {
            headers: {
              Authorization: apiKey,
            },
          },
        },
      ),
    ).pipe(
      map((result) => result.molen as MolenData),
      tap((molen) => {
        this.selectedMolen = molen;
        this.onMolenDataChanged();
      }),
    );
  }

  public uploadImage(
    tbNumber: string,
    image: File,
    apiKey: string,
  ): Observable<MolenData> {
    return fromTypedApi(
      molenUploadImage(
        { tbNumber },
        { image },
        {
          params: {
            headers: {
              Authorization: apiKey,
            },
          },
        },
      ),
    ).pipe(
      map((result) => result.molen as MolenData),
      tap((molen) => {
        this.selectedMolen = molen;
        this.onMolenDataChanged();
      }),
    );
  }

  private onMolenDataChanged(): void {
    CacheManager.clearByPrefix(MolenCacheKeys.prefix);
    this.mapRefreshSubject.next();
    this.refreshMapSummary();
    this.refreshMolensWithImageCount();
  }

  private refreshMapSummary(): void {
    this.getMapSummary().subscribe({
      error: () => {
        // The data-changing operation succeeded; a summary refresh failure
        // should not turn that operation into an error.
      },
    });
  }

  private refreshMolensWithImageCount(): void {
    this.getMolensWithImageCount().subscribe({
      error: () => {
        // Keep the successfully completed image change as the main result.
      },
    });
  }
}
