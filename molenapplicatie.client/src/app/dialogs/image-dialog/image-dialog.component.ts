import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialog, MatDialogRef } from '@angular/material/dialog';
import { DialogReturnStatus } from '../../../Enums/DialogReturnStatus';
import { ConfirmationDialogComponent } from '../confirmation-dialog/confirmation-dialog.component';
import { ConfirmationDialogData } from '../../../Interfaces/ConfirmationDialogData';
import { Toasts } from '../../../Utils/Toasts';
import { DialogReturnType } from '../../../Interfaces/DialogReturnType';
import { HttpClient } from '@angular/common/http';
import { MolenImage } from '../../../Interfaces/Models/MolenImage';

@Component({
  selector: 'app-image-dialog',
  templateUrl: './image-dialog.component.html',
  styleUrl: './image-dialog.component.scss'
})
export class ImageDialogComponent {
  constructor(@Inject(MAT_DIALOG_DATA) public data: { selectedImage: MolenImage, canBeDeleted: boolean },
    private dialogRef: MatDialogRef<ImageDialogComponent>,
    private dialog: MatDialog,
    private toast: Toasts,
    private http: HttpClient) { }

  get image(): MolenImage {
    return this.data.selectedImage;
  }

  getFilePath(): string | undefined {
    if (this.data && this.data.selectedImage) {
      return this.data.selectedImage.filePath;
    }
    return undefined;
  }

  onClose(): void {
    this.dialogRef.close();
  }

  openDeleteDialog(): void {
    if (this.data.canBeDeleted) {
      const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
        data: {
          title: 'Foto verwijderen',
          message: 'Weet je zeker dat je deze foto wilt verwijderen?',
          api_key_usage: true
        } as ConfirmationDialogData
      });

      dialogRef.afterClosed().subscribe({
        next: (result: DialogReturnType) => {
          if (result.status == DialogReturnStatus.Confirmed) {
            this.dialogRef.close({ status: DialogReturnStatus.Deleted, api_key: result.api_key } as DialogReturnType);
          } else {
            this.dialogRef.close(result);
          }
        }
      })
    } else {
      this.toast.showInfo("Deze foto kan niet worden verwijderd!")
    }
  }

  downloadImage(image: MolenImage) {
    this.http.get(image.filePath, { responseType: 'blob' }).subscribe((blob) => {
      const link = document.createElement('a');
      link.href = URL.createObjectURL(blob);
      link.download = image.name;
      link.click();
      URL.revokeObjectURL(link.href);
    });
  }
}
