import {
  ComponentRef,
  Injectable,
  ViewContainerRef,
} from '@angular/core';
import { ToastType } from '../Enums/ToastType';
import { ToastComponent } from '../app/toast/toast.component';

@Injectable({
  providedIn: 'root',
})
export class Toasts {
  private activeToasts: ComponentRef<ToastComponent>[] = [];
  private viewContainerRef!: ViewContainerRef;
  private defaultToastTime = 4000;

  constructor() {}

  showSuccess(
    message: string,
    title?: string,
    duration: number = this.defaultToastTime
  ) {
    this.showToast(title || 'Success', message, ToastType.Success, duration);
  }

  showError(
    message: string,
    title?: string,
    duration: number = this.defaultToastTime
  ) {
    this.showToast(title || 'Error', message, ToastType.Error, duration);
  }

  showInfo(
    message: string,
    title?: string,
    duration: number = this.defaultToastTime
  ) {
    this.showToast(title || 'Informatie', message, ToastType.Info, duration);
  }

  showWarning(
    message: string,
    title?: string,
    duration: number = this.defaultToastTime
  ) {
    this.showToast(
      title || 'Waarschuwing!',
      message,
      ToastType.Warning,
      duration
    );
  }

  setViewContainerRef(vcr: ViewContainerRef) {
    this.viewContainerRef = vcr;
  }

  showToast(
    title: string,
    message: string,
    type: ToastType,
    duration: number = this.defaultToastTime
  ) {
    const toastContainer = document.getElementById('toast-container');

    if (!toastContainer || !this.viewContainerRef) return;

    const componentRef: ComponentRef<ToastComponent> =
      this.viewContainerRef.createComponent(ToastComponent);

    componentRef.instance.title = title;
    componentRef.instance.message = message;
    componentRef.instance.type = type;
    componentRef.instance.duration = duration;

    componentRef.instance.closeToast.subscribe(() => {
      this.clearToast(componentRef);
    });

    toastContainer.appendChild(componentRef.location.nativeElement);
    this.activeToasts.push(componentRef);
  }

  clearAllToasts() {
    this.activeToasts.forEach((toast) => {
      const index = this.viewContainerRef.indexOf(toast.hostView);
      if (index !== -1) {
        this.viewContainerRef.remove(index);
      }
    });
    this.activeToasts = [];
  }

  clearToast(toast: ComponentRef<ToastComponent>) {
    const index = this.viewContainerRef.indexOf(toast.hostView);
    this.clearToastByIndex(index);
  }

  clearToastByIndex(index: number) {
    if (index >= 0 && index < this.activeToasts.length) {
      const toast = this.activeToasts[index];
      if (toast != null) {
        this.viewContainerRef.remove(index);
        this.activeToasts = this.activeToasts.filter((t) => t !== toast);
      }
    }
  }

  removeLastAddedToast() {
    this.clearToastByIndex(this.activeToasts.length - 1);
  }
}
