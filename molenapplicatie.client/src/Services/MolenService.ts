import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, tap, of, catchError, throwError } from 'rxjs';
import { MolenDataClass } from '../Class/MolenDataClass';
import { MolenData } from '../Interfaces/MolenData';
import { Toasts } from '../Utils/Toasts';
import { MapService } from './MapService';

@Injectable({
  providedIn: 'root'
})
export class MolenService {
  public selectedMolenTenBruggeNumber?: string;
  public selectedMolen?: MolenData;
  public molens?: MolenData[];
  private lastUpdatedTimestamp?: number;
  private readonly refreshInterval = 30 * 60 * 1000;

  constructor(private http: HttpClient,
    private toasts: Toasts,
    private mapService: MapService) { }

  public getMolenFromBackend(ten_Brugge_Nr: string): Observable<MolenDataClass> {
    return this.http.get<MolenDataClass>('/api/molen/' + ten_Brugge_Nr);
  }

  public getMolen(ten_Brugge_Nr: string): Observable<MolenDataClass> {
    var molen = this.molens?.find(molen => molen.ten_Brugge_Nr == ten_Brugge_Nr);
    if (molen == undefined) {
      return this.getMolenFromBackend(ten_Brugge_Nr);
    }
    return of(molen);
  }

  public getAllMolens(): Observable<MolenDataClass[]> {
    const currentTime = Date.now();
    const needsRefresh = !this.lastUpdatedTimestamp ||
      (currentTime - this.lastUpdatedTimestamp) > this.refreshInterval;

    if (needsRefresh) {
      return this.http.get<MolenDataClass[]>('/api/all_molens').pipe(
        tap((molens) => {
          this.molens = molens;
          this.lastUpdatedTimestamp = currentTime;
        })
      );
    } else {
      return of(this.molens!);
    }
  }

  public removeSelectedMolen() {
    this.selectedMolenTenBruggeNumber = undefined;
    this.selectedMolen = undefined;
  }

  public getMolenWithImageAmount(): number {
    if (this.molens == undefined) return 0;
    return this.molens.filter(x => x.hasImage).length
  }

  public deleteImage(tbNr: string, imageName: string, APIKey: string): Observable<any> {
    const headers = new HttpHeaders({
      'Authorization': APIKey,
    });

    return this.http.delete<MolenData>("/api/molen_image/" + tbNr + "/" + imageName, { headers }).pipe(
      tap((updatedMolen: MolenData) => {
        if (this.molens) {
          var indexOfMolen: number = this.molens.findIndex(molen => molen.ten_Brugge_Nr == tbNr) ?? -1;
          if (indexOfMolen != -1 && indexOfMolen != 0) {
            var prevMolen = this.molens[indexOfMolen];
            this.molens[indexOfMolen] = updatedMolen;
            this.markerUpdate(tbNr, prevMolen.hasImage, updatedMolen.hasImage);
          }
        }
      }),
      catchError((error) => {
        return throwError(error);
      })
    );
  }

  public uploadImage(tbNr: string, image: FormData, APIKey: string) {
    const headers = new HttpHeaders({
      'Authorization': APIKey,
    });
    return this.http.post<MolenData>(`/api/upload_image/${tbNr}`, image, { headers }).pipe(
      tap((updatedMolen: MolenData) => {
        if (this.molens) {
          var indexOfMolen: number = this.molens.findIndex(molen => molen.ten_Brugge_Nr == tbNr) ?? -1;
          if (indexOfMolen != -1) {
            var prevMolen = this.molens[indexOfMolen];
            this.molens[indexOfMolen] = updatedMolen;
            this.markerUpdate(tbNr, prevMolen.hasImage, updatedMolen.hasImage);
          }
        }
      }),
      catchError((error) => {
        return throwError(error);
      }));
  }

  private markerUpdate(tbn: string, prev: boolean, curr: boolean) {
    if (prev != curr) {
      var molen = this.molens?.find(molen => molen.ten_Brugge_Nr == tbn);
      if (molen) {
        this.mapService.updateMarker(tbn, molen);
      }
    }
  }
}
