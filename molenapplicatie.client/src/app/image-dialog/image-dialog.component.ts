import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialog, MatDialogRef } from '@angular/material/dialog';
import { SafeUrl } from '@angular/platform-browser';
import { MolenImage } from '../../Class/MolenImage';
import { DialogReturnStatus } from '../../Enums/DialogReturnStatus';
import { ConfirmationDialogComponent } from '../confirmation-dialog/confirmation-dialog.component';
import { ConfirmationDialogData } from '../../Interfaces/ConfirmationDialogData';
import { Toasts } from '../../Utils/Toasts';
import { DialogReturnType } from '../../Interfaces/DialogReturnType';

@Component({
  selector: 'app-image-dialog',
  templateUrl: './image-dialog.component.html',
  styleUrl: './image-dialog.component.scss'
})
export class ImageDialogComponent {
  constructor(@Inject(MAT_DIALOG_DATA) public data: { selectedImage: MolenImage, canBeDeleted: boolean },
    private dialogRef: MatDialogRef<ImageDialogComponent>,
    private dialog: MatDialog,
    private toast: Toasts) { }

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

  downloadImage() {
    if (this.data.selectedImage.image) {
      const link = document.createElement('a');
      link.href = this.data.selectedImage.image.toString();
      link.download = this.data.selectedImage.name;

      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
    }
  }
}
