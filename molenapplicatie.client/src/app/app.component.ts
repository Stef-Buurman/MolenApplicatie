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
  constructor(private toastService: Toasts,
    private vcr: ViewContainerRef,
    private http: HttpClient) { }

  ngOnInit() {
    this.toastService.setViewContainerRef(this.vcr);
  }

  test() {
    this.map.setView([5, 4.4], 10);
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
        this.toastService.showSuccess("Molens updated");
      }
    });
  }
}
