import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import {
  distinctUntilChanged,
  filter,
  map,
  Observable,
  skip,
  Subject,
  Subscription,
  switchMap,
  takeUntil,
} from 'rxjs';
import { DialogReturnStatus } from '../../Enums/DialogReturnStatus';
import { DialogReturnType } from '../../Interfaces/DialogReturnType';
import { Toasts } from '../../Utils/Toasts';
import { ImageDialogComponent } from '../dialogs/image-dialog/image-dialog.component';
import { MolenImage } from '../../Interfaces/Models/MolenImage';
import { Router, ActivatedRoute, NavigationEnd } from '@angular/router';
import { merge, of } from 'rxjs';

@Component({
  selector: 'app-image-selector',
  templateUrl: './image-selector.component.html',
  styleUrl: './image-selector.component.scss',
})
export class ImageSelectorComponent implements OnInit {
  @Input() images: MolenImage[] = [];
  @Output() imagesChange = new EventEmitter<MolenImage[]>();
  @Input() selectedImage?: MolenImage;
  @Output() selectedImageChange = new EventEmitter<MolenImage>();
  @Input() tbNr: string = '';
  @Input() deleteFunction!: (
    imgName: string,
    api_key: string
  ) => Observable<any>;
  private destroy$ = new Subject<void>();

  constructor(
    private dialog: MatDialog,
    private toast: Toasts,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    if (this.images.length > 0) this.selectedImageChange.emit(this.images[0]);

    const initialParams = of(
      this.getDeepestChild(this.route).snapshot.paramMap
    );

    const paramsOnNavigation = this.router.events.pipe(
      filter((event) => event instanceof NavigationEnd),
      map(() => this.getDeepestChild(this.route)),
      switchMap((route) => route.paramMap)
    );

    merge(initialParams, paramsOnNavigation)
      .pipe(
        map((paramMap) => paramMap.get('imageName')),
        distinctUntilChanged(),
        takeUntil(this.destroy$)
      )
      .subscribe((imageName) => {
        if (
          imageName &&
          typeof imageName === 'string' &&
          this.images.length > 0
        ) {
          this.changeImage(imageName);
          this.openImage();
        }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private getDeepestChild(route: ActivatedRoute): ActivatedRoute {
    let child = route;
    while (child.firstChild) {
      child = child.firstChild;
    }
    return child;
  }

  openImageDialog(imageName: string): void {
    const dialogRef = this.dialog.open(ImageDialogComponent, {
      data: {
        selectedImage: this.getImageByName(imageName),
        canBeDeleted: true,
        tenBruggeNumber: this.route.snapshot.paramMap.get('TenBruggeNumber'),
      },
    });

    dialogRef.afterClosed().subscribe(() => {
      const tenBruggeNumber =
        this.route.snapshot.paramMap.get('TenBruggeNumber');
      this.router.navigate(['/map', tenBruggeNumber]);
    });
  }

  getImageByName(name: string): MolenImage | undefined {
    return this.images.find((x) => x.name == name);
  }

  changeImage(imgName: string) {
    var newSelectedImage = this.getImageByName(imgName);

    if (newSelectedImage != undefined) {
      this.selectedImage = newSelectedImage;
    }
  }

  isSelectedImage(image: MolenImage): Boolean {
    if (!this.selectedImage) return false;
    return this.selectedImage.name == image.name;
  }

  routeToImage(): void {
    if (this.selectedImage) {
      this.router.navigate([`/map/${this.tbNr}`, this.selectedImage.name]);
    }
  }

  openImage(): void {
    if (this.selectedImage) {
      var selectedImage: MolenImage = this.selectedImage;
      var canBeDeleted: boolean = selectedImage.canBeDeleted;

      const dialogRef = this.dialog.open(ImageDialogComponent, {
        data: {
          selectedImage,
          canBeDeleted,
        },
        panelClass: 'selected-image',
      });

      dialogRef.afterClosed().subscribe((result: DialogReturnType) => {
        this.router.navigate(['/map', this.tbNr]);
        if (
          result &&
          result.status == DialogReturnStatus.Deleted &&
          result.api_key &&
          this.deleteFunction != undefined
        ) {
          this.deleteFunction(selectedImage.name, result.api_key).subscribe({
            error: (error) => {
              if (error.status == 401) {
                this.toast.showError('Er is een verkeerde api key ingevuld!');
              } else {
                this.toast.showError(error.error.message);
              }
            },
            complete: () => {
              this.images = this.images.filter(
                (x) => x.name != selectedImage.name
              );
              this.imagesChange.emit(this.images);
              this.selectedImage = this.images[0];
              this.selectedImageChange.emit(this.images[0]);
              this.toast.showSuccess('De foto is verwijderd!');
            },
          });
        } else if (
          result &&
          result.status == DialogReturnStatus.Deleted &&
          !result.api_key
        ) {
          this.toast.showWarning(
            'Er is geen api key ingevuld, de foto is niet verwijderd!'
          );
        } else if (result && result.status == DialogReturnStatus.Error) {
          this.toast.showError(
            'Er is iets fout gegaan met het verwijderen van de foto!'
          );
        }
      });
    }
  }
}
