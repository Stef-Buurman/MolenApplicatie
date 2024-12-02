import { Component, Input, input, OnInit } from '@angular/core';
import { MatDialogRef } from '@angular/material/dialog';
import { Router, ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-filter-map',
  templateUrl: './filter-map.component.html',
  styleUrl: './filter-map.component.scss'
})
export class FilterMapComponent implements OnInit{
  //@Input() startOption: string = '';
  @Input() closeFunction!: () => void;
  selectedOption: string= ""
  constructor(private router: Router,
    private route: ActivatedRoute,) { }

  ngOnInit() {
    //this.selectedOption = this.startOption;
  }

  close() {
    if (this.closeFunction) {
      this.closeFunction();
    }
  }

  onOptionChange(event: Event) {
    const targetUrl = "/"+this.selectedOption;
    this.router.navigateByUrl(targetUrl);
    this.close();
  }
}
