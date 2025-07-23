import {
  Component,
  ElementRef,
  EventEmitter,
  HostListener,
  Input,
  Output,
} from '@angular/core';
import { Place } from '../../Interfaces/Models/Place';
import { HttpClient } from '@angular/common/http';
import { Toasts } from '../../Utils/Toasts';
import {
  SearchModel,
  SearchResultsModel,
} from '../../Interfaces/SearchResultModel';
import { MolenData } from '../../Interfaces/Models/MolenData';
import { MolenType } from '../../Interfaces/Models/MolenType';
import { GetMolenIcon, GetMolenTypeIcon } from '../../Utils/GetMolenIcon';

@Component({
  selector: 'app-search-bar',
  templateUrl: './search-bar.component.html',
  styleUrl: './search-bar.component.scss',
})
export class SearchBarComponent {
  searchResult!: SearchResultsModel;
  searchTerm: string = '';
  searchFocused = false;
  searchTimeoutRef: any = null;

  handleSearchChange(query: string) {
    this.searchTerm = query;
  }

  clearSearch(inputRef: HTMLInputElement) {
    if (this.searchTimeoutRef) {
      clearTimeout(this.searchTimeoutRef);
    }
    this.searchTerm = '';
    this.searchFocused = false;
    inputRef.focus();
  }

  @Input() selectedPlace!: Place;
  @Output() selectedPlaceChange = new EventEmitter<Place>();
  @Output() selectedMolenChange = new EventEmitter<MolenData>();
  @Output() selectedTypeChange = new EventEmitter<MolenType>();

  isDropdownVisible: boolean = false;
  constructor(
    private http: HttpClient,
    private toastService: Toasts,
    private eRef: ElementRef
  ) {}

  filterOptions() {
    this.isDropdownVisible = true;
    if (this.searchTerm.length > 2) {
      const encodedQuery = encodeURIComponent(this.searchTerm);
      this.http
        .get<SearchResultsModel>(`/api/search?query=${encodedQuery}`)
        .subscribe({
          next: (result) => {
            this.searchResult = result;
          },
          error: (error) => {
            this.toastService.showError(error.error);
          },
        });
    }
  }

  getMolenIcon(molen: MolenData): string {
    const types = molen.molenTypeAssociations
      ? molen.molenTypeAssociations
          .map((type) =>
            type.molenType && type.molenType.name
              ? type.molenType.name.toLocaleLowerCase()
              : ''
          )
          .filter((type) => type !== '')
      : [];

    console.log(molen.molenTypeAssociations);

    return (
      'Assets/Icons/Molens/' +
      GetMolenIcon(molen.toestand, types, molen.hasImage)
    );
  }

  getMolenTypeIcon(molen: MolenType): string {
    console.log(molen);
    return (
      'Assets/Icons/Molens/' +
      GetMolenTypeIcon([molen.name.toLocaleLowerCase()]) +
      '.png'
    );
  }

  selectMolen(option: SearchModel<MolenData>) {
    this.selectedMolenChange.emit(option.data);
    this.isDropdownVisible = false;
    this.searchTerm = option.data.name;
  }

  selectPlace(option: SearchModel<Place>) {
    this.selectedPlaceChange.emit(option.data);
    this.isDropdownVisible = false;
    this.searchTerm = option.data.name;
  }

  selectType(option: SearchModel<MolenType>) {
    this.selectedTypeChange.emit(option.data);
    this.isDropdownVisible = false;
    this.searchTerm = option.data.name;
  }

  onInputClick() {
    this.isDropdownVisible = true;
  }

  @HostListener('document:click', ['$event'])
  clickOutside(event: Event) {
    if (!this.eRef.nativeElement.contains(event.target)) {
      this.isDropdownVisible = false;
      this.searchFocused = false;
    }
  }
}
