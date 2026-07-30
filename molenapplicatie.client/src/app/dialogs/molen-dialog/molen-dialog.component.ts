import { ChangeDetectorRef, Component, Inject } from '@angular/core';
import {
  MatDialogRef,
  MAT_DIALOG_DATA,
  MatDialog,
} from '@angular/material/dialog';
import { EMPTY, Observable } from 'rxjs';
import { MolenData } from '../../../Interfaces/Models/MolenData';
import { Toasts } from '../../../Utils/Toasts';
import { UploadImageDialogComponent } from '../upload-image-dialog/upload-image-dialog.component';
import { DomSanitizer } from '@angular/platform-browser';
import { SecurityContext } from '@angular/core';
import { MolenImage } from '../../../Interfaces/Models/MolenImage';
import { MolenService } from '../../../Services/MolenService';

@Component({
  selector: 'app-molen-dialog',
  standalone: false,
  templateUrl: './molen-dialog.component.html',
  styleUrl: './molen-dialog.component.scss',
})
export class MolenDialogComponent {
  public molen?: MolenData;
  public molenImages: MolenImage[] = [];
  public selectedImage?: MolenImage;
  goToMolenId?: string;

  isExpanded = false;

  deleteImageFunction = this.deleteImage.bind(this);

  constructor(
    private toasts: Toasts,
    private cdr: ChangeDetectorRef,
    private molenService: MolenService,
    private dialogRef: MatDialogRef<MolenDialogComponent>,
    private dialog: MatDialog,

    private sanitizer: DomSanitizer,
    @Inject(MAT_DIALOG_DATA)
    public data: { molenId?: string; molen?: MolenData },
  ) {}

  ngOnInit(): void {
    if (!this.data.molenId && !this.data.molen) {
      this.onClose();
      return;
    }

    if (this.data.molen) {
      this.setMolen(this.data.molen);
      return;
    }

    this.molenService.getMolenById(this.data.molenId!).subscribe({
      next: (molen) => {
        this.setMolen(molen);
      },
      error: (error) => {
        this.toasts.showError(
          error.message ?? 'Molen kon niet worden geladen.',
        );
        this.onClose();
      },
    });
  }

  private setMolen(molen: MolenData): void {
    this.molen = molen;
    this.molenImages = this.getAllMolenImages();
    this.selectedImage = this.molenImages[0];
    this.cdr.detectChanges();
  }

  GoToMolen(molenId?: string | null): void {
    if (!molenId) return;

    this.goToMolenId = molenId;
    this.onClose();
  }

  sanitizeHtml(html: string): string {
    const sanitizedHtml =
      this.sanitizer.sanitize(SecurityContext.NONE, html) || '';
    const parser = new DOMParser();
    const doc = parser.parseFromString(sanitizedHtml, 'text/html');
    return doc.body.textContent || '';
  }

  getBouwjaar(): string {
    if (!this.molen) return '';
    if (this.molen.bouwjaar !== undefined && this.molen.bouwjaar !== null) {
      return this.molen.bouwjaar.toString();
    } else if (
      this.molen.bouwjaarStart !== undefined &&
      this.molen.bouwjaarStart !== null &&
      this.molen.bouwjaarEinde !== undefined &&
      this.molen.bouwjaarEinde !== null
    ) {
      return `${this.molen.bouwjaarStart} - ${this.molen.bouwjaarEinde}`;
    } else if (
      this.molen.bouwjaarStart !== undefined &&
      this.molen.bouwjaarStart !== null
    ) {
      return this.molen.bouwjaarStart.toString();
    } else if (
      this.molen.bouwjaarEinde !== undefined &&
      this.molen.bouwjaarEinde !== null
    ) {
      return this.molen.bouwjaarEinde.toString();
    } else {
      return 'Onbekend';
    }
  }

  onClose(): void {
    this.dialogRef.close(this.goToMolenId);
  }

  expandDetails() {
    this.isExpanded = !this.isExpanded;
  }

  deleteImage(imgName: string, api_key: string): Observable<any> {
    if (!this.molen) {
      return EMPTY;
    }
    return this.molenService.deleteImage(
      this.getMolenImageReference(this.molen),
      imgName,
      api_key,
    );
  }

  private getMolenImageReference(molen: MolenData): string {
    return molen.ten_Brugge_Nr?.trim() || molen.id;
  }

  uploadImage() {
    this.molen;
    const dialogRef = this.dialog.open(UploadImageDialogComponent, {
      data: {
        molen: this.molen,
      },
      panelClass: 'upload-images',
    });

    dialogRef.afterClosed().subscribe((result: MolenData) => {
      if (result) {
        var previousImages = this.molenImages;
        this.cdr.detectChanges();
        this.molen = result;
        this.molenImages = this.getAllMolenImages();
        for (var i = 0; i < this.molenImages.length; i++) {
          var foundImage = previousImages.find(
            (x) => x.name == this.molenImages[i].name,
          );
          if (foundImage == undefined) {
            this.selectedImage = this.molenImages[i];
          }
        }
      }
    });
  }

  getAllMolenImages(): MolenImage[] {
    var AllImages: MolenImage[] = [];
    if (this.molen) {
      if (this.molen.images) {
        AllImages = AllImages.concat(this.molen.images);
      }
      if (this.molen.addedImages && this.molen.addedImages.length > 0) {
        AllImages = AllImages.concat(this.molen.addedImages);
      }
    }
    return AllImages;
  }
}
