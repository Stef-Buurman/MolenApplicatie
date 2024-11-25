import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, tap, of, catchError, throwError } from 'rxjs';
import { MolenDataClass } from '../Class/MolenDataClass';
import { MolenData } from '../Interfaces/MolenData';
import { Toasts } from '../Utils/Toasts';
import { MapService } from './MapService';
import { SavedMolens } from '../Class/SavedMolens';

@Injectable({
  providedIn: 'root'
})
export class MolenService {
  public selectedMolenTenBruggeNumber?: string;
  public selectedMolen?: MolenData;
  public allMolens?: SavedMolens;
  public activeMolens?: SavedMolens;
  public existingMolens?: SavedMolens;
  public desappearedMolens?: SavedMolens;
  public remainderMolens?: SavedMolens;
  private lastUpdatedTimestamp?: number;
  private readonly refreshInterval = 30 * 60 * 1000;

  constructor(private http: HttpClient,
    private toasts: Toasts,
    private mapService: MapService) { }

  public getMolenFromBackend(ten_Brugge_Nr: string): Observable<MolenDataClass> {
    return this.http.get<MolenDataClass>('/api/molen/' + ten_Brugge_Nr);
  }

  public getMolen(ten_Brugge_Nr: string): Observable<MolenDataClass> {
    var molen = this.allMolens?.Molens.find(molen => molen.ten_Brugge_Nr == ten_Brugge_Nr);
    if (molen == undefined) {
      return this.getMolenFromBackend(ten_Brugge_Nr);
    }
    return of(molen);
  }

  public getAllActiveMolens(): Observable<MolenDataClass[]> {
    const currentTime = Date.now();
    const needsRefresh = !this.activeMolens?.LastUpdatedTimestamp ||
      (currentTime - this.activeMolens.LastUpdatedTimestamp) > this.refreshInterval;

    if (needsRefresh) {
      return this.http.get<MolenDataClass[]>('/api/molen/active').pipe(
        tap((activeMolens) => {
          this.activeMolens = new SavedMolens(currentTime,activeMolens);
          this.lastUpdatedTimestamp = currentTime;
        })
      );
    } else {
      return of(this.activeMolens?.Molens!);
    }
  }

  public getAllExistingMolens(): Observable<MolenDataClass[]> {
    const currentTime = Date.now();
    const needsRefresh = !this.existingMolens?.LastUpdatedTimestamp ||
      (currentTime - this.existingMolens.LastUpdatedTimestamp) > this.refreshInterval;

    if (needsRefresh) {
      return this.http.get<MolenDataClass[]>('/api/molen/existing').pipe(
        tap((existingMolens) => {
          this.existingMolens = new SavedMolens(currentTime, existingMolens);
          this.lastUpdatedTimestamp = currentTime;
        })
      );
    } else {
      return of(this.existingMolens?.Molens!);
    }
  }

  public getAllDisappearedMolens(): Observable<MolenDataClass[]> {
    const currentTime = Date.now();
    const needsRefresh = !this.desappearedMolens?.LastUpdatedTimestamp ||
      (currentTime - this.desappearedMolens.LastUpdatedTimestamp) > this.refreshInterval;

    if (needsRefresh) {
      return this.http.get<MolenDataClass[]>('/api/molen/disappeared').pipe(
        tap((desappearedMolens) => {
          this.desappearedMolens = new SavedMolens(currentTime, desappearedMolens);
          this.lastUpdatedTimestamp = currentTime;
        })
      );
    } else {
      return of(this.desappearedMolens?.Molens!);
    }
  }

  public getAllRemainderMolens(): Observable<MolenDataClass[]> {
    const currentTime = Date.now();
    const needsRefresh = !this.remainderMolens?.LastUpdatedTimestamp ||
      (currentTime - this.remainderMolens.LastUpdatedTimestamp) > this.refreshInterval;

    if (needsRefresh) {
      return this.http.get<MolenDataClass[]>('/api/molen/remainder').pipe(
        tap((remainderMolens) => {
          this.remainderMolens = new SavedMolens(currentTime, remainderMolens);
          this.lastUpdatedTimestamp = currentTime;
        })
      );
    } else {
      return of(this.remainderMolens?.Molens!);
    }
  }

  public getAllMolens(): Observable<MolenDataClass[]> {
    const currentTime = Date.now();
    const needsRefresh = !this.lastUpdatedTimestamp ||
      (currentTime - this.lastUpdatedTimestamp) > this.refreshInterval;

    if (needsRefresh) {
      return this.http.get<MolenDataClass[]>('/api/molen/all').pipe(
        tap((molens) => {
          this.allMolens = new SavedMolens(currentTime, molens);
          this.lastUpdatedTimestamp = currentTime;
        })
      );
    } else {
      return of(this.allMolens?.Molens!);
    }
  }

  public removeSelectedMolen() {
    this.selectedMolenTenBruggeNumber = undefined;
    this.selectedMolen = undefined;
  }

  public getMolenWithImageAmount(): number {
    if (this.allMolens == undefined) return 0;
    return this.allMolens.Molens.filter(x => x.hasImage).length
  }

  public deleteImage(tbNr: string, imageName: string, APIKey: string): Observable<any> {
    const headers = new HttpHeaders({
      'Authorization': APIKey,
    });

    return this.http.delete<MolenData>("/api/molen/molen_image/" + tbNr + "/" + imageName, { headers }).pipe(
      tap((updatedMolen: MolenData) => {
        if (this.allMolens) {
          var indexOfMolen: number = this.allMolens.Molens.findIndex(molen => molen.ten_Brugge_Nr == tbNr) ?? -1;
          if (indexOfMolen != -1 && indexOfMolen != 0) {
            var prevMolen = this.allMolens.Molens[indexOfMolen];
            this.allMolens.Molens[indexOfMolen] = updatedMolen;
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
    return this.http.post<MolenData>(`/api/molen/molen_image/${tbNr}`, image, { headers }).pipe(
      tap((updatedMolen: MolenData) => {
        if (this.allMolens) {
          var indexOfMolen: number = this.allMolens.Molens.findIndex(molen => molen.ten_Brugge_Nr == tbNr) ?? -1;
          if (indexOfMolen != -1) {
            var prevMolen = this.allMolens.Molens[indexOfMolen];
            this.allMolens.Molens[indexOfMolen] = updatedMolen;
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
      var molen = this.allMolens?.Molens.find(molen => molen.ten_Brugge_Nr == tbn);
      if (molen) {
        this.mapService.updateMarker(tbn, molen);
      }
    }
  }
}
