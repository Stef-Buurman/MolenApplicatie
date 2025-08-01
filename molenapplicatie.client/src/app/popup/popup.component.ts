import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { RecentAddedImages } from '../../Interfaces/MolensResponseType';
import { ActivatedRoute, Router } from '@angular/router';
import { MolenImage } from '../../Interfaces/Models/MolenImage';
import { MolenData } from '../../Interfaces/Models/MolenData';
import { MapData } from '../../Interfaces/Map/MapData';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-popup',
  templateUrl: './popup.component.html',
  styleUrls: ['./popup.component.scss'],
})
export class PopupComponent implements OnInit {
  @Input() molenImages?: RecentAddedImages[];
  @Input() visible: boolean = true;
  @Output() visibleChange: EventEmitter<boolean> = new EventEmitter<boolean>();
  @Input() onMolenChange?: (
    selectedMolen: MolenData,
    navigate?: boolean
  ) => Observable<MapData[]>;

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

  goToMolen(molen: MolenData) {
    this.onMolenChange?.(molen)?.subscribe();
  }

  goToImage(molen: MolenData, image: MolenImage) {
    this.onMolenChange?.(molen)?.subscribe({
      complete: () => {
        this.router.navigate([molen.ten_Brugge_Nr, image.name], {
          relativeTo: this.route,
        });
      },
    });
  }
}
