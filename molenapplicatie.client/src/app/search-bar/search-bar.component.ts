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
import {
  Subject,
  debounceTime,
  distinctUntilChanged,
  tap,
  switchMap,
} from 'rxjs';

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
  isloading: boolean = false;

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

  searchSubject = new Subject<string>();

  ngOnInit(): void {
    this.searchSubject
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        tap(() => {
          this.isloading = true;
          this.isDropdownVisible = true;
        }),
        switchMap((term) => {
          if (term.length <= 2) return [];
          const encodedQuery = encodeURIComponent(term);
          return this.http.get<SearchResultsModel>(
            `/api/search?query=${encodedQuery}`
          );
        })
      )
      .subscribe({
        next: (result: any) => {
          const term = this.searchTerm;

          if (result.molens) {
            result.molens = result.molens.map(
              (item: { reference: string }) => ({
                ...item,
                reference: this.highlightReference(item.reference, term),
              })
            );
          }

          if (result.places) {
            result.places = result.places.map(
              (placeGroup: { key: any; value: any[] }) => ({
                key: placeGroup.key,
                value: placeGroup.value.map((item: { reference: string }) => ({
                  ...item,
                  reference: this.highlightReference(item.reference, term),
                })),
              })
            );
          }

          if (result.molenTypes) {
            result.molenTypes = result.molenTypes.map(
              (item: { reference: string }) => ({
                ...item,
                reference: this.highlightReference(item.reference, term),
              })
            );
          }

          this.searchResult = result;
          this.isloading = false;
        },
        error: (err) => {
          this.toastService.showError(err.error);
        },
      });
  }
  filterOptions() {
    this.searchSubject.next(this.searchTerm);
  }

  hasResults(): boolean {
    return (
      (this.searchResult &&
        (this.searchResult.molens?.length > 0 ||
          this.searchResult.places?.length > 0 ||
          this.searchResult.molenTypes?.length > 0)) ||
      this.isloading
    );
  }

  highlightReference(text: string, term: string): string {
    if (!term || term.length < 2) return text;

    const escapedTerm = term.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    const regex = new RegExp(`(${escapedTerm})`, 'gi');
    return text.replace(regex, '<mark>$1</mark>');
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

    return (
      'Assets/Icons/Molens/' +
      GetMolenIcon(molen.toestand, types, molen.hasImage)
    );
  }

  getMolenTypeIcon(molen: MolenType): string {
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
