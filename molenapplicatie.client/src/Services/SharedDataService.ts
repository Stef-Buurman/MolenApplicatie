import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class SharedDataService {
  private _IsLoading = new BehaviorSubject<boolean>(true);
  public IsLoading$ = this._IsLoading.asObservable();

  public get IsLoading() {
    return this._IsLoading.value;
  }

  public set IsLoading(value: boolean) {
    this._IsLoading.next(value);
  }

  public IsLoadingTrue() {
    this._IsLoading.next(true);
  }

  public IsLoadingFalse() {
    this._IsLoading.next(false);
  }
}
