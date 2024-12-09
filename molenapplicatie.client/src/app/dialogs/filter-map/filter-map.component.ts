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

  onOptionChange(event: Event) {
    const targetUrl = "/" + this.selectedOption;
    this.router.navigateByUrl(targetUrl);
    this.onClose();
  }
}
