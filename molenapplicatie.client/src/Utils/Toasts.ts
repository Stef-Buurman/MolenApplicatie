import { ApplicationRef, ComponentFactoryResolver, ComponentRef, Injectable, Injector, Renderer2, RendererFactory2, ViewContainerRef } from '@angular/core';
import { Subject } from 'rxjs';
import { ToastType } from '../Enums/ToastType';
import { Toast } from '../Interfaces/Toast';
import { BehaviorSubject } from 'rxjs';
import { ToastComponent } from '../app/toast/toast.component';

@Injectable({
  providedIn: 'root'
})
export class Toasts {
  private renderer: Renderer2;

  constructor(
    private rendererFactory: RendererFactory2,
    private appRef: ApplicationRef,
    private injector: Injector
  ) {
    this.renderer = this.rendererFactory.createRenderer(null, null);
  }

  showSuccess(message: string, title?: string) {
    this.showToast(title || 'Success', message, ToastType.Success);
  }

  showError(message: string, title?: string) {
    this.showToast(title || 'Error', message, ToastType.Error);
  }

  showInfo(message: string, title?: string) {
    this.showToast(title || 'Informatie', message, ToastType.Info);
  }

  showWarning(message: string, title?: string) {
    this.showToast(title || 'Waarschuwing!', message, ToastType.Warning);
  }

  setViewContainerRef(vcr: ViewContainerRef) {
    this.viewContainerRef = vcr;
  }

  private viewContainerRef!: ViewContainerRef;

  showToast(title: string, message: string, type: ToastType, duration: number = 3000) {
    const toastContainer = document.getElementById('toast-container');

    if (!toastContainer || !this.viewContainerRef) return;

    const componentRef: ComponentRef<ToastComponent> = this.viewContainerRef.createComponent(ToastComponent);

    componentRef.instance.title = title;
    componentRef.instance.message = message;
    componentRef.instance.type = type;
    componentRef.instance.duration = duration;

    componentRef.instance.closeToast.subscribe(() => {
      this.viewContainerRef.remove(this.viewContainerRef.indexOf(componentRef.hostView));
    });

    toastContainer.appendChild(componentRef.location.nativeElement);

    // Attach the component view to the application
    this.appRef.attachView(componentRef.hostView);
  }
}
