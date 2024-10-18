import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialog, MatDialogRef } from '@angular/material/dialog';
import { SafeUrl } from '@angular/platform-browser';
import { MolenImage } from '../../Class/MolenImage';
import { DialogReturnStatus } from '../../Enums/DialogReturnStatus';
import { ConfirmationDialogComponent } from '../confirmation-dialog/confirmation-dialog.component';
import { ConfirmationDialogData } from '../../Interfaces/ConfirmationDialogData';

@Component({
  selector: 'app-image-dialog',
  templateUrl: './image-dialog.component.html',
  styleUrl: './image-dialog.component.scss'
})
export class ImageDialogComponent {
  constructor(@Inject(MAT_DIALOG_DATA) public data: {selectedImage: MolenImage},
    private dialogRef: MatDialogRef<ImageDialogComponent>,
    private dialog: MatDialog) { }

  onClose(): void {
    this.dialogRef.close();
  }

  openDeleteDialog(): void {
    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      data:{
        title: 'Delete image',
        message: 'Are you sure you want to delete this image?',
        api_key_usage: false
      } as ConfirmationDialogData
    });

    dialogRef.afterClosed().subscribe({
      next: (result: boolean) => {
        if (result) {
          this.dialogRef.close({ status: DialogReturnStatus.Deleted });
        }
      }
    })
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
