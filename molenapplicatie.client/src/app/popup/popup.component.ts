import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { RecentAddedImages } from '../../Interfaces/MolensResponseType';
import { Router } from '@angular/router';
import { MolenData, MolenImage } from '../../api/generated/data-contracts';

@Component({
  selector: 'app-popup',
  standalone: false,
  templateUrl: './popup.component.html',
  styleUrls: ['./popup.component.scss'],
})
export class PopupComponent implements OnInit {
  @Input() molenImages?: RecentAddedImages[];
  @Input() visible: boolean = true;
  @Output() visibleChange: EventEmitter<boolean> = new EventEmitter<boolean>();
  @Input() onMolenChange?: (
    selectedMolen: MolenData,
    navigate?: boolean,
  ) => void;

  currentIndex: number = 0;

  constructor(private router: Router) {}

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

  goToMolen(molen: MolenData) {
    this.onMolenChange?.(molen);
  }

  goToImage(molen: MolenData, image: MolenImage) {
    this.onMolenChange?.(molen, false);
    this.router.navigate(['/map', molen.id, image.name]);
  }
}
