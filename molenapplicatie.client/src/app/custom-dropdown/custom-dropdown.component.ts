import {
  Component,
  Input,
  Output,
  EventEmitter,
  ElementRef,
  ViewChild,
  OnDestroy,
  AfterViewInit,
  TemplateRef,
} from '@angular/core';
import { Overlay, OverlayRef } from '@angular/cdk/overlay';
import { TemplatePortal } from '@angular/cdk/portal';
import { ViewContainerRef } from '@angular/core';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-custom-dropdown',
  templateUrl: './custom-dropdown.component.html',
  styleUrls: ['./custom-dropdown.component.scss'],
})
export class CustomDropdownComponent implements AfterViewInit, OnDestroy {
  @Input() options: { name: string; count?: number }[] = [];
  @Input() placeholder: string = 'Selecteer';
  @Input() selected: string = '';
  @Output() selectedChange = new EventEmitter<string>();

  @ViewChild('dropdownTemplate') dropdownTemplate!: TemplateRef<any>;
  @ViewChild('triggerRef') triggerRef!: ElementRef;

  overlayRef!: OverlayRef;
  isOpen = false;
  private backdropSub!: Subscription;

  constructor(private overlay: Overlay, private vcr: ViewContainerRef) {}

  ngAfterViewInit(): void {}

  toggleDropdown() {
    if (this.isOpen) {
      this.closeDropdown();
    } else {
      const triggerRect = this.triggerRef.nativeElement.getBoundingClientRect();

      const positionStrategy = this.overlay
        .position()
        .global()
        .top(`${triggerRect.bottom + window.scrollY}px`)
        .left(`${triggerRect.left + window.scrollX}px`);

      this.overlayRef = this.overlay.create({
        positionStrategy,
        hasBackdrop: true,
        backdropClass: 'transparent-backdrop',
        scrollStrategy: this.overlay.scrollStrategies.reposition(),
        panelClass: 'custom-dropdown-panel',
        width: triggerRect.width, // ✅ Match width
      });

      const dropdownPortal = new TemplatePortal(this.dropdownTemplate, this.vcr);
      this.overlayRef.attach(dropdownPortal);

      this.backdropSub = this.overlayRef
        .backdropClick()
        .subscribe(() => this.closeDropdown());

      this.isOpen = true;
    }
  }

  select(value: string) {
    this.selected = value;
    this.selectedChange.emit(value);
    this.closeDropdown();
  }

  closeDropdown() {
    if (this.overlayRef) {
      this.overlayRef.detach();
      this.backdropSub?.unsubscribe();
    }
    this.isOpen = false;
  }

  ngOnDestroy(): void {
    this.closeDropdown();
  }
}
