import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import {
  distinctUntilChanged,
  filter,
  map,
  merge,
  Observable,
  of,
  Subject,
  switchMap,
  takeUntil,
} from 'rxjs';
import { DialogReturnStatus } from '../../Enums/DialogReturnStatus';
import { DialogReturnType } from '../../Interfaces/DialogReturnType';
import { Toasts } from '../../Utils/Toasts';
import { getTypedApiErrorMessage } from '../../Utils/TypedApiObservable';
import { ImageDialogComponent } from '../dialogs/image-dialog/image-dialog.component';
import { Router, ActivatedRoute, NavigationEnd } from '@angular/router';
import { AddedImage } from '../../api/generated/data-contracts';
import { MolenImageType } from '../../Interfaces/Models/MolenImageType';

@Component({
  selector: 'app-image-selector',
  standalone: false,
  templateUrl: './image-selector.component.html',
  styleUrl: './image-selector.component.scss',
})
export class ImageSelectorComponent implements OnInit {
  @Input() images: MolenImageType[] = [];
  @Output() imagesChange = new EventEmitter<MolenImageType[]>();

  @Input() selectedImage?: MolenImageType;
  @Output() selectedImageChange = new EventEmitter<MolenImageType>();

  @Input() molenId: string = '';

  @Input() deleteFunction!: (
    imgName: string,
    api_key: string,
  ) => Observable<unknown>;

  private destroy$ = new Subject<void>();

  constructor(
    private dialog: MatDialog,
    private toast: Toasts,
    private router: Router,
    private route: ActivatedRoute,
  ) {}

  ngOnInit(): void {
    if (this.images.length > 0) {
      this.selectedImage = this.images[0];
      this.selectedImageChange.emit(this.images[0]);
    }

    const initialParams = of(
      this.getDeepestChild(this.route).snapshot.paramMap,
    );

    const paramsOnNavigation = this.router.events.pipe(
      filter((event) => event instanceof NavigationEnd),
      map(() => this.getDeepestChild(this.route)),
      switchMap((route) => route.paramMap),
    );

    merge(initialParams, paramsOnNavigation)
      .pipe(
        map((paramMap) => paramMap.get('imageName')),
        distinctUntilChanged(),
        takeUntil(this.destroy$),
      )
      .subscribe((imageName) => {
        if (imageName && this.images.length > 0) {
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

  getImageByName(name: string): MolenImageType | undefined {
    return this.images.find((x) => x.name === name);
  }

  changeImage(imgName: string): void {
    const newSelectedImage = this.getImageByName(imgName);

    if (newSelectedImage) {
      this.selectedImage = newSelectedImage;
      this.selectedImageChange.emit(newSelectedImage);
    }
  }

  isSelectedImage(image: MolenImageType): boolean {
    if (!this.selectedImage) {
      return false;
    }

    return this.selectedImage.name === image.name;
  }

  isAddedImage(image: MolenImageType): image is AddedImage {
    return 'dateTaken' in image;
  }

  routeToImage(): void {
    if (this.selectedImage) {
      this.router.navigate(['/map', this.molenId, this.selectedImage.name]);
    }
  }

  openImage(): void {
    if (!this.selectedImage) {
      return;
    }

    const selectedImage = this.selectedImage;
    const canBeDeleted = selectedImage.canBeDeleted;

    const dialogRef = this.dialog.open(ImageDialogComponent, {
      data: {
        selectedImage,
        canBeDeleted,
      },
      panelClass: 'selected-image',
    });

    dialogRef.afterClosed().subscribe((result: DialogReturnType) => {
      this.router.navigate(['/map', this.molenId]);

      if (
        result &&
        result.status === DialogReturnStatus.Deleted &&
        result.api_key &&
        this.deleteFunction !== undefined
      ) {
        this.deleteFunction(selectedImage.name, result.api_key).subscribe({
          error: (error) => {
            if (error.status === 401) {
              this.toast.showError('Er is een verkeerde api key ingevuld!');
            } else {
              this.toast.showError(getTypedApiErrorMessage(error));
            }
          },
          complete: () => {
            this.images = this.images.filter(
              (x) => x.name !== selectedImage.name,
            );

            this.imagesChange.emit(this.images);

            this.selectedImage = this.images[0];

            if (this.selectedImage) {
              this.selectedImageChange.emit(this.selectedImage);
            }

            this.toast.showSuccess('De foto is verwijderd!');
          },
        });
      } else if (
        result &&
        result.status === DialogReturnStatus.Deleted &&
        !result.api_key
      ) {
        this.toast.showWarning(
          'Er is geen api key ingevuld, de foto is niet verwijderd!',
        );
      } else if (result && result.status === DialogReturnStatus.Error) {
        this.toast.showError(
          'Er is iets fout gegaan met het verwijderen van de foto!',
        );
      }
    });
  }
}
