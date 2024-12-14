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
  public minimumWaitTime:number = 1000
  get error(): boolean {
    return this.errors.HasError;
  }
  get isLoadingVisible() {
    return this.sharedData.IsLoading;
  }
  constructor(private toasts: Toasts,
    private vcr: ViewContainerRef,
    private errors: ErrorService,
    private sharedData: SharedDataService) { }

  ngOnInit() {
    this.toasts.setViewContainerRef(this.vcr);
  }
}
