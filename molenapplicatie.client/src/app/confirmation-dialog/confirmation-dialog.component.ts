import { HttpClient } from '@angular/common/http';
import { Component, Inject, Input, OnDestroy } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MolenImage } from '../../Class/MolenImage';
import { ConfirmationDialogData } from '../../Interfaces/ConfirmationDialogData';
import { Toasts } from '../../Utils/Toasts';
import { DialogReturnType } from '../../Interfaces/DialogReturnType';
import { DialogReturnStatus } from '../../Enums/DialogReturnStatus';

@Component({
  selector: 'app-confirmation-dialog',
  templateUrl: './confirmation-dialog.component.html',
  styleUrl: './confirmation-dialog.component.scss'
})
export class ConfirmationDialogComponent implements OnDestroy {
  public APIKey: string = "";
  private confirmation: boolean = false;
  private onCloseCalled: boolean = false;
  constructor(@Inject(MAT_DIALOG_DATA) public data: ConfirmationDialogData,
    private dialogRef: MatDialogRef<ConfirmationDialogComponent>,
    private toasts: Toasts,
    private http: HttpClient) { }

  onClose(isSure: boolean | undefined = undefined): void {
    this.onCloseCalled = true;

    if (isSure == undefined) {
      isSure = this.confirmation;
    }

    if (!isSure) {
      this.toasts.showInfo("Er is niets veranderd!");
      this.dialogRef.close({ status: DialogReturnStatus.Cancelled } as DialogReturnType);
    } else {
      this.dialogRef.close({ status: DialogReturnStatus.Confirmed, api_key: this.APIKey } as DialogReturnType);
    }
  }

  confirm(): void {
    this.confirmation = true;
    this.onClose();
  }

  ngOnDestroy(): void {
    if (!this.onCloseCalled) this.onClose();
  }
}
