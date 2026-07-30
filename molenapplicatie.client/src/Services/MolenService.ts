import { Injectable } from '@angular/core';
import { BehaviorSubject, map, Observable, Subject, tap } from 'rxjs';
import { MolenData } from '../Interfaces/Models/MolenData';
import { MolenFilterList } from '../Interfaces/Filters/MolenFilterList';
import { RecentAddedImages } from '../Interfaces/MolensResponseType';
import {
  molenDeleteMolenImage,
  molenGetMapSummary,
  molenGetMolenDataById,
  molenGetMolenFilters,
  molenGetNewAddedMolens,
  molenUpdateOldestMolens,
  molenUploadImage,
} from '../api/methods/Molen.api';
import { fromTypedApi } from '../Utils/TypedApiObservable';

export interface MolenMapSummary {
  totalMolensWithImage: number;
  recentAddedImages: RecentAddedImages[];
}

@Injectable({
  providedIn: 'root',
})
export class MolenService {
  public selectedMolen?: MolenData;

  private readonly mapSummarySubject = new BehaviorSubject<MolenMapSummary>({
    totalMolensWithImage: 0,
    recentAddedImages: [],
  });
  public readonly mapSummary$ = this.mapSummarySubject.asObservable();

  private readonly mapRefreshSubject = new Subject<void>();
  public readonly mapRefresh$ = this.mapRefreshSubject.asObservable();

  public getMolenById(id: string): Observable<MolenData> {
    return fromTypedApi(molenGetMolenDataById({ id })).pipe(
      map((molen) => molen as MolenData),
      tap((molen) => {
        this.selectedMolen = molen;
      }),
    );
  }

  public getAllMolenFilters(): Observable<MolenFilterList> {
    return fromTypedApi(molenGetMolenFilters()).pipe(
      map((filters) => filters as MolenFilterList),
    );
  }

  public getMapSummary(): Observable<MolenMapSummary> {
    return fromTypedApi(molenGetMapSummary()).pipe(
      map((summary) => summary as MolenMapSummary),
      tap((summary) => {
        this.mapSummarySubject.next(summary);
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
    this.mapRefreshSubject.next();
    this.refreshMapSummary();
  }

  private refreshMapSummary(): void {
    this.getMapSummary().subscribe({
      error: () => {
        // The data-changing operation succeeded; a summary refresh failure
        // should not turn that operation into an error.
      },
    });
  }
}
