import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { RecentAddedImages } from '../../Interfaces/MolensResponseType';
import { ActivatedRoute, Router } from '@angular/router';
import { MolenImage } from '../../Interfaces/Models/MolenImage';

@Component({
  selector: 'app-popup',
  templateUrl: './popup.component.html',
  styleUrls: ['./popup.component.scss'],
})
export class PopupComponent implements OnInit {
  @Input() molenImages?: RecentAddedImages[];
  @Input() visible: boolean = true;
  @Output() visibleChange: EventEmitter<boolean> = new EventEmitter<boolean>();

  currentIndex: number = 0;

  constructor(private router: Router, private route: ActivatedRoute) {}

  dismiss() {
    this.visible = false;
    this.visibleChange.emit(this.visible);
  }

  next() {
    if (this.molenImages && this.currentIndex < this.molenImages.length - 1) {
      this.currentIndex++;
    }
  }

  previous() {
    if (this.currentIndex > 0) {
      this.currentIndex--;
    }
  }

  ngOnInit() {
    if (this.molenImages) {
      this.currentIndex = 0;
    }
  }

  goToMolen(tenBruggeNr: string) {
    this.router.navigate([tenBruggeNr], {
      relativeTo: this.route,
    });
  }

  goToImage(tenBruggeNr: string, image: MolenImage) {
    this.router.navigate([tenBruggeNr, image.name], {
      relativeTo: this.route,
    });
  }
}
