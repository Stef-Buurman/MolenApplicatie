import {
  AfterViewInit,
  ChangeDetectorRef,
  Component,
  ElementRef,
  Inject,
  ViewChild,
} from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MolenData } from '../../../Interfaces/Models/MolenData';
import { Toasts } from '../../../Utils/Toasts';
import { MolenImage } from '../../../Interfaces/Models/MolenImage';
import { MolenService } from '../../../Services/MolenService';

@Component({
  selector: 'app-upload-image-dialog',
  templateUrl: './upload-image-dialog.component.html',
  styleUrl: './upload-image-dialog.component.scss',
})
export class UploadImageDialogComponent implements AfterViewInit {
  public molen?: MolenData;
  public status: 'initial' | 'uploading' | 'success' | 'fail' = 'initial';
  public file: File | null = null;
  public imagePreview: string | null = null;
  public APIKey: string = '';
  @ViewChild('fileUpload') fileUpload!: ElementRef;

  public imagesAdded: boolean = false;
  get HasImagesLeft(): boolean {
    if (this.molen == undefined || this.molen.addedImages == undefined)
      return false;
    return this.molen.addedImages.length > 0;
  }

  isExpanded = false;

  constructor(
    private toasts: Toasts,
    private cdr: ChangeDetectorRef,
    private molenService: MolenService,
    private dialogRef: MatDialogRef<UploadImageDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { molen: MolenData }
  ) {}

  ngAfterViewInit(): void {
    this.molen = this.data.molen;
    this.fileUpload.nativeElement.click();
  }

  ngOnDestroy(): void {
    this.onClose();
  }

  onClose(): void {
    this.dialogRef.close(this.molen);
  }

  expandDetails() {
    this.isExpanded = !this.isExpanded;
  }

  removeImg(): void {
    this.file = null;
  }

  onFileSelected(event: any): void {
    const uploadedFile: File = event.target.files[0];

    if (uploadedFile) {
      this.status = 'initial';
      this.file = uploadedFile;
      const reader = new FileReader();
      reader.onload = (e) => {
        this.imagePreview = e.target?.result as string;
      };
      reader.readAsDataURL(this.file);
    }
  }

  onSubmit(): void {
    if (this.file && this.molen) {
      this.status = 'uploading';

      const formData = new FormData();
      formData.append('image', this.file, this.file.name);
      this.molenService
        .uploadImage(this.molen.ten_Brugge_Nr, formData, this.APIKey)
        .subscribe({
          next: (molen: MolenData) => {
            this.molen = molen;
            this.removeImg();
            this.APIKey = '';
            this.toasts.showSuccess('Image is saved successfully!');
            this.onClose();
          },
          error: (error) => {
            this.status = 'fail';
            if (error.status == 401) {
              this.toasts.showError('Er is een verkeerde api key ingevuld!');
            } else {
              this.toasts.showError(error.error.message);
            }
          },
        });
    }
  }
}
