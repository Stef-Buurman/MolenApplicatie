import { Component, OnInit, ViewContainerRef, } from '@angular/core';
import { SharedDataService } from '../Services/SharedDataService';
import { Toasts } from '../Utils/Toasts';
import { ErrorService } from '../Services/ErrorService';


@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit {
  get error(): boolean {
    return this.errors.HasError;
  }
  private _isLoadingVisible = true;
  get isLoadingVisible() {
    return this._isLoadingVisible || this.sharedData.IsLoading;
  }
  set isLoadingVisible(value: boolean) {
    this._isLoadingVisible = value;
  }
  constructor(private toasts: Toasts,
    private vcr: ViewContainerRef,
    private errors: ErrorService,
    private sharedData: SharedDataService) { }

  ngOnInit() {
    this.toasts.setViewContainerRef(this.vcr);
    setTimeout(() => {
      this.isLoadingVisible = false;
    }, 1250);
  }
}
