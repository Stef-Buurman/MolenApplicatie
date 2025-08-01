import { ChangeDetectorRef, Component, Inject, OnDestroy } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialog } from '@angular/material/dialog';
import { Observable, of } from 'rxjs';
import { MolenDataClass } from '../../../Class/MolenDataClass';
import { MolenImage } from '../../../Class/MolenImage';
import { MolenData } from '../../../Interfaces/MolenData';
import { MolenService } from '../../../Services/MolenService';
import { Toasts } from '../../../Utils/Toasts';
import { UploadImageDialogComponent } from '../upload-image-dialog/upload-image-dialog.component';
import { DomSanitizer } from '@angular/platform-browser';
import { SecurityContext } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-molen-dialog',
  templateUrl: './molen-dialog.component.html',
  styleUrl: './molen-dialog.component.scss'
})
export class MolenDialogComponent implements OnDestroy{
  public molen?: MolenDataClass;
  public status: "initial" | "uploading" | "success" | "fail" = "initial";
  public file: File | null = null;
  public imagePreview: string | null = null;
  public APIKey: string = "";
  public molenImages: MolenImage[] = [];
  public selectedImage?: MolenImage;

  public imagesAdded: boolean = false;
  get HasImagesLeft(): boolean {
    if (this.molen == undefined || this.molen.addedImages == undefined) return false;
    return this.molen.addedImages.length > 0;
  }

  goToMolenTBN!: string;

  isExpanded = false;

  deleteImageFunction = this.deleteImage.bind(this);

  constructor(private toasts: Toasts,
    private cdr: ChangeDetectorRef,
    private molenService: MolenService,
    private dialogRef: MatDialogRef<MolenDialogComponent>,
    private dialog: MatDialog,
    private sanitizer: DomSanitizer,
    private router: Router,
    private route: ActivatedRoute,
    @Inject(MAT_DIALOG_DATA) public data: { tenBruggeNr: string , molen:MolenData}
  ) { }

  ngOnInit(): void {
    if (!this.data.molen && !this.data.tenBruggeNr) {
      this.onClose();
    }
    if (this.data.molen) {
      this.molen = this.data.molen;
      this.molenImages = this.getAllMolenImages();
      this.selectedImage = this.molenImages[0];
    }
    if (this.data.tenBruggeNr) {
      this.molenService.getMolen(this.data.tenBruggeNr).subscribe({
        next: (molen: MolenDataClass) => {
          this.molen = molen;
          this.molenImages = this.getAllMolenImages();
          this.selectedImage = this.molenImages[0];
        },
        error: (error) => {
          this.toasts.showError(error.error.message);
        }
      })
    }
  }

  GoToMolen(TBN: string) {
    this.goToMolenTBN = TBN;
    this.onClose();
  }

  sanitizeHtml(html: string): string {
    const sanitizedHtml = this.sanitizer.sanitize(SecurityContext.NONE, html) || '';
    const parser = new DOMParser();
    const doc = parser.parseFromString(sanitizedHtml, 'text/html');
    return doc.body.textContent || '';
  }

  getBouwjaar(): string {
    if (!this.molen) return "";
    if (this.molen.bouwjaar !== undefined && this.molen.bouwjaar !== null) {
      return this.molen.bouwjaar.toString();
    } else if (this.molen.bouwjaarStart !== undefined && this.molen.bouwjaarStart !== null && this.molen.bouwjaarEinde !== undefined && this.molen.bouwjaarEinde !== null) {
      return `${this.molen.bouwjaarStart} - ${this.molen.bouwjaarEinde}`;
    } else if (this.molen.bouwjaarStart !== undefined && this.molen.bouwjaarStart !== null) {
      return this.molen.bouwjaarStart.toString();
    } else if (this.molen.bouwjaarEinde !== undefined && this.molen.bouwjaarEinde !== null) {
      return this.molen.bouwjaarEinde.toString();
    } else {
      return "Onbekend";
    }
  }

  ngOnDestroy(): void {
    this.onClose();
  }

  onClose(): void {
    this.dialogRef.close(this.goToMolenTBN);
  }

  expandDetails() {
    this.isExpanded = !this.isExpanded
  }

  removeImg(): void {
    this.file = null;
  }

  deleteImage(imgName: string, api_key: string): Observable<any> {
    if (!this.molen || !this.molen.ten_Brugge_Nr) {
      return of();
    }
    return this.molenService.deleteImage(this.molen.ten_Brugge_Nr, imgName, api_key);
  }

  onFileSelected(event: any): void {
    const uploadedFile: File = event.target.files[0];

    if (uploadedFile) {
      this.status = "initial";
      this.file = uploadedFile;
      const reader = new FileReader();
      reader.onload = (e) => {
        this.imagePreview = e.target?.result as string;
      };
      reader.readAsDataURL(this.file);
    }
  }

  uploadImage() {
    this.molen
    const dialogRef = this.dialog.open(UploadImageDialogComponent, {
      data: {
        molen: this.molen
      },
      panelClass: 'upload-images'
    });

    dialogRef.afterClosed().subscribe((result: MolenData) => {
      if (result) {
        var previousImages = this.molenImages;
        this.cdr.detectChanges();
        this.molen = result;
        this.molenImages = this.getAllMolenImages();
        for (var i = 0; i < this.molenImages.length; i++) {
          var foundImage = previousImages.find(x => x.name == this.molenImages[i].name);
          if (foundImage == undefined) {
            this.selectedImage = this.molenImages[i];
          }
        }
      }
    });
  }

  onSubmit(): void {
    if (this.file && this.molen) {
      this.status = "uploading";

      const formData = new FormData();
      formData.append('images', this.file, this.file.name);

      var previousMolenImages: MolenImage[] = this.getAllMolenImages();

      this.molenService.uploadImage(this.molen.ten_Brugge_Nr, formData, this.APIKey).subscribe({
          next: (molen: MolenData) => {
            this.molen = molen;
            this.cdr.detectChanges();
          },
          error: (error) => {
            this.status = "fail";
            if (error.status == 401) {
              this.toasts.showError("Er is een verkeerde api key ingevuld!");
            } else {
              this.toasts.showError(error.error.message);
            }
          },
          complete: () => {
            this.removeImg();
            this.molenImages = this.getAllMolenImages();
            this.selectedImage = this.molenImages.find(x => !previousMolenImages.find(y => y.name == x.name));
            this.toasts.showSuccess("Image is saved successfully!");
            this.imagesAdded = true;
          }
        });
      this.APIKey = "";
    }
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
