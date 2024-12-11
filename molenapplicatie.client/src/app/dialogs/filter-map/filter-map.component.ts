import { Component, Inject, Input, input, OnInit } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { Router, ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-filter-map',
  templateUrl: './filter-map.component.html',
  styleUrl: './filter-map.component.scss'
})
export class FilterMapComponent implements OnInit {
  selectedOption: string = "";
  provincies: string[] = ["Drenthe", "Flevoland", "Friesland", "Gelderland", "Groningen", "Limburg", "Noord-Brabant", "Noord-Holland", "Overijssel", "Utrecht", "Zeeland", "Zuid-Holland"];
  provincie: string = "";
  constructor(@Inject(MAT_DIALOG_DATA) public data: { selectedOption: string, provincie: string },
    private dialogRef: MatDialogRef<FilterMapComponent>,

    private router: Router,
    private route: ActivatedRoute) { }

  ngOnInit() {
    if (this.router.url) {
      this.selectedOption = this.router.url.replace("/", "");
    }
  }

  onClose() {
    this.dialogRef.close();
  }

  filterMap() {
    const targetUrl = "/" + this.selectedOption;
    this.router.navigateByUrl(targetUrl);
    this.onClose();
  }
}
