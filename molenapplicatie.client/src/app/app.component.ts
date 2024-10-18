import { Component, OnInit, ViewContainerRef } from '@angular/core';
import { Toasts } from '../Utils/Toasts';
import { ToastType } from '../Enums/ToastType';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit {
  constructor(private toastService: Toasts, private vcr: ViewContainerRef) { }

  ngOnInit() {
    this.toastService.setViewContainerRef(this.vcr);
  }
}
