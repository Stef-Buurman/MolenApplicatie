import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, tap, of } from 'rxjs';
import { MolenDataClass } from '../Class/MolenDataClass';
import { MolenData } from '../Interfaces/MolenData';
import { Toasts } from '../Utils/Toasts';

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
    private toasts: Toasts) { }

  public getMolenFromBackend(ten_Brugge_Nr: string): Observable<MolenDataClass> {
    return this.http.get<MolenDataClass>('/api/molen/' + ten_Brugge_Nr);
  }

  public getMolen(ten_Brugge_Nr: string): MolenData | undefined {
    return this.molens?.find(molen => molen.ten_Brugge_Nr == ten_Brugge_Nr);
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
}
