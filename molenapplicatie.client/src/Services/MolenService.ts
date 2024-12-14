import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { catchError, Observable, of, tap, throwError } from 'rxjs';
import { MolenDataClass } from '../Class/MolenDataClass';
import { SavedMolens } from '../Class/SavedMolens';
import { MolenData } from '../Interfaces/MolenData';
import { Toasts } from '../Utils/Toasts';
import { MapService } from './MapService';

@Injectable({
  providedIn: 'root'
})
export class MolenService {
  public selectedMolenTenBruggeNumber?: string;
  public selectedMolen?: MolenData;
  public allMolens?: SavedMolens;
  public activeMolens?: SavedMolens;
  public existingMolens?: SavedMolens;
  public disappearedMolens: { [key: string]: SavedMolens } = {};
  public remainderMolens?: SavedMolens;
  private lastUpdatedTimestamp?: number;
  private readonly refreshInterval = 30 * 60 * 1000;
  private allMolenProvincies: string[] = []; 

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

  public getAllMolenProvincies(): Observable<string[]> {
    const currentTime = Date.now();
    const needsRefresh = this.allMolenProvincies.length == 0;

    if (needsRefresh) {
      return this.http.get<string[]>('/api/molen/provincies').pipe(
        tap((activeMolens:string[]) => {
          this.allMolenProvincies = activeMolens;
        })
      );
    } else {
      return of(this.allMolenProvincies);
    }
  }

  public getAllActiveMolens(): Observable<MolenDataClass[]> {
    const currentTime = Date.now();
    const needsRefresh = !this.activeMolens?.LastUpdatedTimestamp ||
      (currentTime - this.activeMolens.LastUpdatedTimestamp) > this.refreshInterval;

    if (needsRefresh) {
      return this.http.get<MolenDataClass[]>('/api/molen/active').pipe(
        tap((activeMolens) => {
          this.activeMolens = new SavedMolens(currentTime, activeMolens);
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

  public getDisappearedMolensByProvincie(provincie: string): Observable<MolenDataClass[]> {
    const currentTime = Date.now();
    const provincieMolens = this.disappearedMolens[provincie];
    const needsRefresh = (!provincieMolens?.LastUpdatedTimestamp ||
      (currentTime - provincieMolens.LastUpdatedTimestamp) > this.refreshInterval) || provincieMolens == undefined;

    if (needsRefresh) {
      return this.http.get<MolenDataClass[]>('/api/molen/disappeared/' + provincie).pipe(
        tap((desappearedMolens) => {
          this.disappearedMolens[provincie] = new SavedMolens(currentTime, desappearedMolens);
          this.lastUpdatedTimestamp = currentTime;
        })
      );
    } else {
      return of(this.disappearedMolens[provincie]?.Molens!);
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
    var molensCounted: string[] = [];
    if (this.allMolens != undefined) {
      var molensWithImage = this.getMolenTBNWithImages(this.allMolens.Molens);
      for (let i = 0; i < molensWithImage.length; i++) {
        if (!molensCounted.includes(molensWithImage[i])) {
          molensCounted.push(molensWithImage[i]);
        }
      }
    }
    if (this.activeMolens != undefined) {
      var molensWithImage = this.getMolenTBNWithImages(this.activeMolens.Molens);
      for (let i = 0; i < molensWithImage.length; i++) {
        if (!molensCounted.includes(molensWithImage[i])) {
          molensCounted.push(molensWithImage[i]);
        }
      }
    }
    if (this.existingMolens != undefined) {
      var molensWithImage = this.getMolenTBNWithImages(this.existingMolens.Molens);
      for (let i = 0; i < molensWithImage.length; i++) {
        if (!molensCounted.includes(molensWithImage[i])) {
          molensCounted.push(molensWithImage[i]);
        }
      }
    }
    if (this.disappearedMolens != undefined) {
      for (const provincie in this.disappearedMolens) {
        var molensWithImage = this.getMolenTBNWithImages(this.disappearedMolens[provincie].Molens);
        for (let i = 0; i < molensWithImage.length; i++) {
          if (!molensCounted.includes(molensWithImage[i])) {
            molensCounted.push(molensWithImage[i]);
          }
        }
      }
    }
    if (this.remainderMolens != undefined) {
      var molensWithImage = this.getMolenTBNWithImages(this.remainderMolens.Molens);
      for (let i = 0; i < molensWithImage.length; i++) {
        if (!molensCounted.includes(molensWithImage[i])) {
          molensCounted.push(molensWithImage[i]);
        }
      }
    }
    return molensCounted.length;
  }

  private getMolenTBNWithImages(molens: MolenData[]): string[] {
    var molensCounted: string[] = [];
    molens.forEach(molen => {
      if (molen.hasImage && !molensCounted.includes(molen.ten_Brugge_Nr)) {
        molensCounted.push(molen.ten_Brugge_Nr);
      }
    }
    );
    return molensCounted;
  }

  public deleteImage(tbNr: string, imageName: string, APIKey: string): Observable<any> {
    const headers = new HttpHeaders({
      'Authorization': APIKey,
    });

    return this.http.delete<MolenData>("/api/molen/molen_image/" + tbNr + "/" + imageName, { headers }).pipe(
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
      'Authorization': APIKey,
    });
    return this.http.post<MolenData>(`/api/molen/molen_image/${tbNr}`, image, { headers }).pipe(
      tap((updatedMolen: MolenData) => {
        this.updateMolen(updatedMolen);
      }),
      catchError((error) => {
        return throwError(error);
      }));
  }

  private updateMolen(updatedMolen: MolenData) {
    if (this.allMolens) {
      var indexOfMolen: number = this.allMolens.Molens.findIndex(molen => molen.ten_Brugge_Nr == updatedMolen.ten_Brugge_Nr) ?? -1;
      if (indexOfMolen != -1) {
        var prevMolen = this.allMolens.Molens[indexOfMolen];
        this.allMolens.Molens[indexOfMolen] = updatedMolen;
        this.markerUpdate(updatedMolen, prevMolen.hasImage, updatedMolen.hasImage);
      }
    }
    if (this.activeMolens) {
      var indexOfMolen: number = this.activeMolens.Molens.findIndex(molen => molen.ten_Brugge_Nr == updatedMolen.ten_Brugge_Nr) ?? -1;
      if (indexOfMolen != -1) {
        var prevMolen = this.activeMolens.Molens[indexOfMolen];
        this.activeMolens.Molens[indexOfMolen] = updatedMolen;
        this.markerUpdate(updatedMolen, prevMolen.hasImage, updatedMolen.hasImage);
      }
    }
    if (this.existingMolens) {
      var indexOfMolen: number = this.existingMolens.Molens.findIndex(molen => molen.ten_Brugge_Nr == updatedMolen.ten_Brugge_Nr) ?? -1;
      if (indexOfMolen != -1) {
        var prevMolen = this.existingMolens.Molens[indexOfMolen];
        this.existingMolens.Molens[indexOfMolen] = updatedMolen;
        this.markerUpdate(updatedMolen, prevMolen.hasImage, updatedMolen.hasImage);
      }
    }
    if (this.disappearedMolens) {
      for (const provincie in this.disappearedMolens) {
        var indexOfMolen: number = this.disappearedMolens[provincie].Molens.findIndex(molen => molen.ten_Brugge_Nr == updatedMolen.ten_Brugge_Nr) ?? -1;
        if (indexOfMolen != -1) {
          var prevMolen = this.disappearedMolens[provincie].Molens[indexOfMolen];
          this.disappearedMolens[provincie].Molens[indexOfMolen] = updatedMolen;
          this.markerUpdate(updatedMolen, prevMolen.hasImage, updatedMolen.hasImage);
        }
      }
    }
    if (this.remainderMolens) {
      var indexOfMolen: number = this.remainderMolens.Molens.findIndex(molen => molen.ten_Brugge_Nr == updatedMolen.ten_Brugge_Nr) ?? -1;
      if (indexOfMolen != -1) {
        var prevMolen = this.remainderMolens.Molens[indexOfMolen];
        this.remainderMolens.Molens[indexOfMolen] = updatedMolen;
        this.markerUpdate(updatedMolen, prevMolen.hasImage, updatedMolen.hasImage);
      }
    }
  }

  private markerUpdate(molen: MolenData, prev: boolean, curr: boolean) {
    if (prev != curr) {
      if (molen) {
        this.mapService.updateMarker(molen.ten_Brugge_Nr, molen);
      }
    }
  }
}
