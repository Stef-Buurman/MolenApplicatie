import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { catchError, map, Observable, of, tap, throwError } from 'rxjs';
import { SavedMolens } from '../Class/SavedMolens';
import { MolenData } from '../Interfaces/Models/MolenData';
import { MolensResponseType } from '../Interfaces/MolensResponseType';
import { MapData } from '../Interfaces/Map/MapData';
import { FilterFormValues } from '../Interfaces/Filters/Filter';
import { BuildFilterQuery } from '../Utils/BuildFilterQuery';
import { MolenFilterList } from '../Interfaces/Filters/MolenFilterList';
import { MapService } from './MapService';

@Injectable({
  providedIn: 'root',
})
export class MolenService {
  public selectedMolenTenBruggeNumber?: string;
  public selectedMolen?: MolenData;
  public allMolens?: SavedMolens;
  public activeMolens?: SavedMolens;
  public existingMolens?: SavedMolens;
  public disappearedMolens: { [key: string]: SavedMolens } = {};
  public remainderMolens?: SavedMolens;
  private allMolenProvincies: string[] = [];
  private _molensWithImageAmount: number | undefined;
  private set molensWithImageAmount(value: number | undefined) {
    this._molensWithImageAmount = value;
  }
  public get molensWithImageAmount(): number | undefined {
    return this._molensWithImageAmount;
  }
  private molenFilters!: MolenFilterList;

  constructor(private http: HttpClient, private mapService: MapService) {}

  public getMolenFromBackend(ten_Brugge_Nr: string): Observable<MolenData> {
    return this.http.get<MolenData>('/api/molen/' + ten_Brugge_Nr);
  }

  public getAllMolenFilters(): Observable<MolenFilterList> {
    return this.http.get<MolenFilterList>('/api/molen/filters').pipe(
      tap((filters: MolenFilterList) => {
        this.molenFilters = filters;
      })
    );
  }

  public getMolen(ten_Brugge_Nr: string): Observable<MolenData> {
    var molen = this.allMolens?.Molens.find(
      (molen) => molen.ten_Brugge_Nr == ten_Brugge_Nr
    );
    if (molen == undefined) {
      return this.getMolenFromBackend(ten_Brugge_Nr);
    }
    return of(molen);
  }

  public getAllMolenProvincies(): Observable<string[]> {
    const needsRefresh = this.allMolenProvincies.length == 0;

    if (needsRefresh) {
      return this.http.get<string[]>('/api/molen/provincies').pipe(
        tap((activeMolens: string[]) => {
          this.allMolenProvincies = activeMolens;
        })
      );
    } else {
      return of(this.allMolenProvincies);
    }
  }

  public getMapData(filters: FilterFormValues[]): Observable<MapData[]> {
    return this.http
      .get<MolensResponseType<MapData>>(
        '/api/molen/mapdata' + BuildFilterQuery(filters)
      )
      .pipe(
        tap((molensResponseType) => {
          this.molensWithImageAmount = molensResponseType.totalMolensWithImage;
        }),
        map((molensResponseType) => molensResponseType.molens)
      );
  }

  public removeSelectedMolen() {
    this.selectedMolenTenBruggeNumber = undefined;
    this.selectedMolen = undefined;
  }

  public deleteImage(
    tbNr: string,
    imageName: string,
    APIKey: string
  ): Observable<any> {
    const headers = new HttpHeaders({
      Authorization: APIKey,
    });

    return this.http
      .delete<MolenData>('/api/molen/molen_image/' + tbNr + '/' + imageName, {
        headers,
      })
      .pipe(
        tap((updatedMolen: MolenData) => {
          this.updateMolen(updatedMolen);
        }),
        catchError((error) => {
          return throwError(error);
        })
      );
  }

  public uploadImage(tbNr: string, image: FormData, APIKey: string) {
    const headers = new HttpHeaders({
      Authorization: APIKey,
    });
    return this.http
      .post<MolenData>(`/api/molen/molen_image/${tbNr}`, image, { headers })
      .pipe(
        tap((updatedMolen: MolenData) => {
          this.updateMolen(updatedMolen);
        }),
        catchError((error) => {
          return throwError(error);
        })
      );
  }

  private updateMolen(updatedMolen: MolenData) {
    if (this.allMolens) {
      var indexOfMolen: number =
        this.allMolens.Molens.findIndex(
          (molen) => molen.ten_Brugge_Nr == updatedMolen.ten_Brugge_Nr
        ) ?? -1;
      if (indexOfMolen != -1) {
        var prevMolen = this.allMolens.Molens[indexOfMolen];
        this.allMolens.Molens[indexOfMolen] = updatedMolen;
        this.markerUpdate(
          updatedMolen,
          prevMolen.hasImage,
          updatedMolen.hasImage
        );
      }
    }
    if (this.activeMolens) {
      var indexOfMolen: number =
        this.activeMolens.Molens.findIndex(
          (molen) => molen.ten_Brugge_Nr == updatedMolen.ten_Brugge_Nr
        ) ?? -1;
      if (indexOfMolen != -1) {
        var prevMolen = this.activeMolens.Molens[indexOfMolen];
        this.activeMolens.Molens[indexOfMolen] = updatedMolen;
        this.markerUpdate(
          updatedMolen,
          prevMolen.hasImage,
          updatedMolen.hasImage
        );
      }
    }
    if (this.existingMolens) {
      var indexOfMolen: number =
        this.existingMolens.Molens.findIndex(
          (molen) => molen.ten_Brugge_Nr == updatedMolen.ten_Brugge_Nr
        ) ?? -1;
      if (indexOfMolen != -1) {
        var prevMolen = this.existingMolens.Molens[indexOfMolen];
        this.existingMolens.Molens[indexOfMolen] = updatedMolen;
        this.markerUpdate(
          updatedMolen,
          prevMolen.hasImage,
          updatedMolen.hasImage
        );
      }
    }
    if (this.disappearedMolens) {
      for (const provincie in this.disappearedMolens) {
        var indexOfMolen: number =
          this.disappearedMolens[provincie].Molens.findIndex(
            (molen) => molen.ten_Brugge_Nr == updatedMolen.ten_Brugge_Nr
          ) ?? -1;
        if (indexOfMolen != -1) {
          var prevMolen =
            this.disappearedMolens[provincie].Molens[indexOfMolen];
          this.disappearedMolens[provincie].Molens[indexOfMolen] = updatedMolen;
          this.markerUpdate(
            updatedMolen,
            prevMolen.hasImage,
            updatedMolen.hasImage
          );
        }
      }
    }
    if (this.remainderMolens) {
      var indexOfMolen: number =
        this.remainderMolens.Molens.findIndex(
          (molen) => molen.ten_Brugge_Nr == updatedMolen.ten_Brugge_Nr
        ) ?? -1;
      if (indexOfMolen != -1) {
        var prevMolen = this.remainderMolens.Molens[indexOfMolen];
        this.remainderMolens.Molens[indexOfMolen] = updatedMolen;
        this.markerUpdate(
          updatedMolen,
          prevMolen.hasImage,
          updatedMolen.hasImage
        );
      }
    }
  }

  private markerUpdate(molen: MolenData, prev: boolean, curr: boolean) {
    if (prev != curr) {
      if (molen) {
        // this.mapService.updateMarker(molen.ten_Brugge_Nr, {
        //   latitude: molen.latitude,
        //   longitude: molen.longitude,
        //   reference: molen.ten_Brugge_Nr,
        //   toestand: molen.toestand,
        //   types: molen.types,
        //   hasImage: molen.hasImage,
        // });
      }
    }
  }
}
