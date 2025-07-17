import { Component, ElementRef, EventEmitter, HostListener, Input, Output } from '@angular/core';
import { Place } from '../../Interfaces/Models/Place';
import { HttpClient } from '@angular/common/http';
import { Toasts } from '../../Utils/Toasts';

@Component({
  selector: 'app-dropdown',
  templateUrl: './dropdown.component.html',
  styleUrl: './dropdown.component.scss'
})
export class DropdownComponent {
  options: Place[] = [];
  filteredOptions: Place[] = [];

  previousSearchTerm: string = '';
  searchTerm: string = '';

  @Input() selectedPlace!: Place;
  @Output() selectedPlaceChange = new EventEmitter<Place>();

  isDropdownVisible: boolean = false;
  constructor(private http: HttpClient,
    private toastService: Toasts,
    private eRef: ElementRef) {
    this.filteredOptions = this.options;
  }

  filterOptions() {
    this.isDropdownVisible = true;
    if (this.searchTerm.length == 2 && this.previousSearchTerm.length == 1) {
      this.http.get<Place[]>(`/api/get_places_by_input/${this.searchTerm}`).subscribe({
        next: (result) => {
          this.options = result
        },
        error: (error) => {
          this.toastService.showError(error.error);
        },
        complete: () => {
          const term = this.searchTerm.toLowerCase();
          this.filteredOptions = this.options.filter(option =>
            option.name.toLowerCase().includes(term)
          );
        }
      });
    } else {
      const term = this.searchTerm.toLowerCase();
      this.filteredOptions = this.options.filter(option =>
        option.name.toLowerCase().includes(term)
      ).sort(x=> x.population);
    }
    this.previousSearchTerm = this.searchTerm;
  }

  selectOption(option: Place) {
    this.selectedPlaceChange.emit(option);
    this.isDropdownVisible = false;
    this.filteredOptions = this.options;
    this.searchTerm = option.name;
  }

  onInputClick() {
    this.isDropdownVisible = true;
  }

  @HostListener('document:click', ['$event'])
  clickOutside(event: Event) {
    if (!this.eRef.nativeElement.contains(event.target)) {
      this.isDropdownVisible = false;
    }
  }
}
