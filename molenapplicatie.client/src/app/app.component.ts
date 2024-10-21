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
  private NewMolensLastExecutionTime: number | null = null;
  private UpdateLastExecutionTime: number | null = null;
  private readonly cooldownTime = 10 * 60 * 1000;
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
    const currentTime = Date.now();
    if (this.UpdateLastExecutionTime && currentTime - this.UpdateLastExecutionTime < this.cooldownTime) {
      this.toastService.showWarning("Dit kan eens elke 10 minuten!");
      return;
    }

    this.toastService.showInfo("Molens worden geupdate... (Dit kan even duren)", "Informatie", 60000);
    this.http.get<MolenData[]>('/api/update_oldest_molens').subscribe({
      next: (result) => {

      },
      error: (error) => {
        console.log(error);
        this.toastService.removeLastAddedToast();
        this.toastService.showError(error.error);
        this.UpdateLastExecutionTime = null
      },
      complete: () => {
        this.toastService.removeLastAddedToast();
        this.toastService.showSuccess("Molens zijn geupdate!");
      }
    });

    this.UpdateLastExecutionTime = currentTime;
  }

  searchForNewMolens() {
    const currentTime = Date.now();
    if (this.NewMolensLastExecutionTime && currentTime - this.NewMolensLastExecutionTime < this.cooldownTime) {
      this.toastService.showWarning("Dit kan eens elke 10 minuten!");
      return;
    }

    this.toastService.showInfo("Nieuwe molens worden gezocht... (Dit kan even duren)", "Informatie", 60000);
    this.http.get<MolenData[]>('/api/search_for_new_molens').subscribe({
      next: (result:MolenData[]) => {
        console.log(result);
        this.toastService.removeLastAddedToast();
        if (result.length == 0) {
          this.toastService.showInfo("Er zijn geen nieuwe molens gevonden!");
        }
        else if (result.length == 1) {
          this.toastService.showSuccess("Er is " + result.length + " nieuwe molen gevonden!");
          this.map.setView([result[0].north, result[0].east], 13);
        }
        else {
          this.toastService.showSuccess("Er zijn " + result.length + " nieuwe molens gevonden!");
        }
      },
      error: (error) => {
        console.log(error);
        this.toastService.removeLastAddedToast();
        this.toastService.showError(error.error);
        this.NewMolensLastExecutionTime = null;
      }
    });

    //this.NewMolensLastExecutionTime = currentTime;
  }
}
