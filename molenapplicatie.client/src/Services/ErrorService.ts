import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ErrorService {
  private errorSubject = new BehaviorSubject<string>("");
  public error$ = this.errorSubject.asObservable();
  public get HasError() {
    return this.errorSubject.value !== "";
  }

  AddError(error: string) {
    this.errorSubject.next(error);
  }
}
