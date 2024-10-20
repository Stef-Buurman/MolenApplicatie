import { Component, OnInit, ViewContainerRef } from '@angular/core';
import { Toasts } from '../Utils/Toasts';
import { ToastType } from '../Enums/ToastType';
import { HttpClient } from '@angular/common/http';
import { MolenData } from '../Interfaces/MolenData';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit {
  map: any;
  visible: boolean = false;
  error: boolean = false;
  constructor(private toastService: Toasts,
    private vcr: ViewContainerRef,
    private http: HttpClient) { }

  ngOnInit() {
    this.toastService.setViewContainerRef(this.vcr);
  }

  test() {
    this.map.setView([51.6914369, 4.2138731], 13);
    //http://api.geonames.org/searchJSON?country=NL&maxRows=500&username=weetikveel12321
  }

  openInfoMenu() {
    this.visible = !this.visible;
  }

  updateMolens() {
    this.toastService.showInfo("Molens worden geupdate...", "Informatie", 10000);
    this.http.get<MolenData[]>('/api/update_oldest_molens').subscribe({
      next: (result) => {

      },
      error: (error) => {
        console.log(error);
        this.toastService.removeLastAddedToast();
        this.toastService.showError(error.error);
      },
      complete: () => {
        this.toastService.removeLastAddedToast();
        this.toastService.showSuccess("Molens zijn geupdate!");
      }
    });
  }

  searchForNewMolens() {
    this.toastService.showInfo("Nieuwe molens worden gezocht...", "Informatie", 10000);
    this.http.get<MolenData[]>('/api/search_for_new_molens').subscribe({
      next: (result) => {
        console.log(result);
        this.toastService.removeLastAddedToast();
        if (result.length == 0) {
          this.toastService.showInfo("Er zijn geen nieuwe molens gevonden!");
        }
        else if (result.length == 1) {
          this.toastService.showSuccess("Er is " + result.length + " nieuwe molen gevonden!");
        }
        else {
          this.toastService.showSuccess("Er zijn " + result.length + " nieuwe molens gevonden!");
        }
      },
      error: (error) => {
        console.log(error);
        this.toastService.removeLastAddedToast();
        this.toastService.showError(error.error);
      }
    });
  }
}
