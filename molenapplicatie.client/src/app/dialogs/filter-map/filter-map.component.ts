import { Component, Inject, OnInit } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { FilterFormValues } from '../../../Interfaces/Filters/Filter';
import {
  MolenFilterList,
  ValueName,
} from '../../../Interfaces/Filters/MolenFilterList';
import { MolenFilters } from '../../../Interfaces/Filters/MolenFilters';
import { MolenService } from '../../../Services/MolenService';

@Component({
  selector: 'app-filter-map',
  standalone: false,
  templateUrl: './filter-map.component.html',
  styleUrl: './filter-map.component.scss',
})
export class FilterMapComponent implements OnInit {
  selectedFilter: MolenFilters = {
    provincie: '',
    toestand: '',
    type: '',
    hasImage: '',
  };

  molenFilters: MolenFilterList = {
    provincies: [],
    toestanden: [],
    types: [],
  };

  imageOptions: { name: string }[] = [
    { name: 'Met foto' },
    { name: 'Zonder foto' },
  ];

  private filters: Record<string, FilterFormValues> = {};

  constructor(
    private dialogRef: MatDialogRef<FilterMapComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { filters: FilterFormValues[] },
    private molenService: MolenService,
  ) {}

  ngOnInit(): void {
    for (const filter of this.data.filters ?? []) {
      this.filters[filter.filterName] = { ...filter };
    }

    this.selectedFilter.provincie = this.getStringFilterValue('Provincie');
    this.selectedFilter.toestand = this.getStringFilterValue('MolenState');
    this.selectedFilter.type = this.getStringFilterValue('MolenType');

    const hasImage = this.filters['HasImage']?.value;
    this.selectedFilter.hasImage =
      hasImage === true ? 'Met foto' : hasImage === false ? 'Zonder foto' : '';

    this.molenService.getAllMolenFilters().subscribe({
      next: (filters) => {
        this.molenFilters = {
          provincies: this.getUniqueOptions(filters.provincies),
          toestanden: this.getUniqueOptions(filters.toestanden),
          types: this.getUniqueOptions(filters.types),
        };
      },
    });
  }

  onClose(filters?: FilterFormValues[]): void {
    this.dialogRef.close(filters);
  }

  filterMap(): void {
    this.setStringFilter(
      'MolenState',
      'Toestand',
      this.selectedFilter.toestand,
    );
    this.setStringFilter(
      'Provincie',
      'Provincie',
      this.selectedFilter.provincie,
    );
    this.setStringFilter('MolenType', 'Molentype', this.selectedFilter.type);

    if (this.selectedFilter.hasImage === 'Met foto') {
      this.setBooleanFilter('HasImage', 'Foto', true);
    } else if (this.selectedFilter.hasImage === 'Zonder foto') {
      this.setBooleanFilter('HasImage', 'Foto', false);
    } else {
      delete this.filters['HasImage'];
    }

    this.onClose(Object.values(this.filters));
  }

  removeFilters(): void {
    this.selectedFilter = {
      provincie: '',
      toestand: '',
      type: '',
      hasImage: '',
    };
    this.filters = {};
    this.onClose([]);
  }

  private getStringFilterValue(filterName: string): string {
    const value = this.filters[filterName]?.value;
    return typeof value === 'string' ? value : '';
  }

  private getUniqueOptions(options: ValueName[]): ValueName[] {
    const uniqueOptions = new Map<string, ValueName>();

    for (const option of options ?? []) {
      const name = option.name?.trim();
      if (!name) continue;

      const key = name.toLocaleLowerCase('nl-NL');
      const existingOption = uniqueOptions.get(key);

      if (!existingOption || option.count > existingOption.count) {
        uniqueOptions.set(key, {
          name,
          count: option.count,
        });
      }
    }

    return Array.from(uniqueOptions.values()).sort((left, right) =>
      left.name.localeCompare(right.name, 'nl-NL'),
    );
  }

  private setStringFilter(
    filterName: string,
    name: string,
    value: string,
  ): void {
    const normalizedValue = value?.trim();

    if (!normalizedValue) {
      delete this.filters[filterName];
      return;
    }

    this.filters[filterName] = {
      filterName,
      value: normalizedValue,
      type: 'string',
      isAList: false,
      name,
    };
  }

  private setBooleanFilter(
    filterName: string,
    name: string,
    value: boolean,
  ): void {
    this.filters[filterName] = {
      filterName,
      value,
      type: 'boolean',
      isAList: false,
      name,
    };
  }
}
