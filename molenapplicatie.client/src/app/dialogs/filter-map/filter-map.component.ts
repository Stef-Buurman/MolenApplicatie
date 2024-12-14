import { Component, Inject, Input, input, OnInit } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { Router, ActivatedRoute } from '@angular/router';
import { Toasts } from '../../../Utils/Toasts';
import { MolenService } from '../../../Services/MolenService';

@Component({
  selector: 'app-filter-map',
  templateUrl: './filter-map.component.html',
  styleUrl: './filter-map.component.scss'
})
export class FilterMapComponent implements OnInit {
  selectedOption: string = "";
  provincies: string[] = [];
  provincie: string = "";
  constructor(@Inject(MAT_DIALOG_DATA) public data: { selectedOption: string, provincie: string },
    private dialogRef: MatDialogRef<FilterMapComponent>,
    private toasts: Toasts,
    private router: Router,
    private molenService:MolenService,
    private route: ActivatedRoute) { }

  ngOnInit() {
    this.molenService.getAllMolenProvincies().subscribe({
      next: (provincies:string[]) => {
        this.provincies = provincies;
        if (this.router.url) {
          var urlStructure = this.router.url.split("/").filter(str => str !== "");
          if (urlStructure.length == 1) {
            this.selectedOption = urlStructure[0];
          } else if (urlStructure.length >= 2) {
            this.selectedOption = urlStructure[0];
            this.provincie = urlStructure[1].toLowerCase();
          }
        }
      }
    })
  }

  onClose() {
    this.dialogRef.close();
  }

  filterMap() {
    var targetUrl = "/" + this.selectedOption;
    if (this.selectedOption == "disappeared") {
      if (this.provincie) {
        targetUrl += "/" + this.provincie
      } else {
        this.toasts.showError("Je hebt geen provincie gekozen!")
        return
      }
    }
    this.router.navigateByUrl(targetUrl);
    this.onClose();
  }
}
