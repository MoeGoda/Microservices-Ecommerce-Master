import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges, forwardRef, signal } from '@angular/core';
import { ControlValueAccessor, FormControl, NG_VALUE_ACCESSOR, ReactiveFormsModule } from '@angular/forms';
import { MatAutocompleteModule, MatAutocompleteSelectedEvent } from '@angular/material/autocomplete';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';

// M — a drop-in replacement for `<mat-select>` on any master-data list
// that can grow large (items, suppliers, locations, categories, units):
// a text input filters the option list as you type. Built on
// MatAutocompleteModule (already ships with @angular/material — no new
// dependency, no custom CDK overlay to maintain) rather than a plain
// mat-select, which has no search affordance at all. Implements
// ControlValueAccessor so it drops into an existing
// `formControlName="itemId"` binding exactly like the mat-select it
// replaces — the FormGroup's value type (typically a numeric id) is
// unchanged; this component only adds the search UI on top.
@Component({
  selector: 'app-searchable-select',
  imports: [ReactiveFormsModule, MatAutocompleteModule, MatFormFieldModule, MatIconModule, MatInputModule, TranslatePipe],
  templateUrl: './searchable-select.component.html',
  styleUrl: './searchable-select.component.scss',
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => SearchableSelectComponent),
      multi: true,
    },
  ],
})
export class SearchableSelectComponent<T> implements ControlValueAccessor, OnChanges {
  @Input() options: readonly T[] = [];
  @Input({ required: true }) optionLabel!: (option: T) => string;
  @Input({ required: true }) optionValue!: (option: T) => unknown;
  @Input() label?: string;
  @Input() placeholder = '';

  @Output() readonly selectionChange = new EventEmitter<T | null>();

  readonly searchControl = new FormControl('', { nonNullable: true });
  readonly filteredOptions = signal<readonly T[]>([]);

  private selectedValue: unknown = null;
  private onChange: (value: unknown) => void = () => {};
  private onTouched: () => void = () => {};

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['options']) {
      this.applyFilter(this.searchControl.value);
      if (this.selectedValue != null) {
        this.syncDisplayFromValue();
      }
    }
  }

  onInput(value: string): void {
    this.applyFilter(value);
  }

  onOptionSelected(event: MatAutocompleteSelectedEvent): void {
    const option = event.option.value as T;
    this.selectedValue = this.optionValue(option);
    this.searchControl.setValue(this.optionLabel(option), { emitEvent: false });
    this.onChange(this.selectedValue);
    this.onTouched();
    this.selectionChange.emit(option);
  }

  onBlur(): void {
    this.onTouched();
    // Typing without landing on a real option shouldn't leave a
    // half-typed value sitting next to a stale selection — revert the
    // visible text back to whatever is actually selected, same as a
    // native <mat-select> would never show an unselected label at all.
    const stillValid = this.options.some((o) => this.optionLabel(o) === this.searchControl.value);
    if (!stillValid) {
      this.syncDisplayFromValue();
    }
  }

  writeValue(value: unknown): void {
    this.selectedValue = value;
    this.syncDisplayFromValue();
  }

  registerOnChange(fn: (value: unknown) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    if (isDisabled) {
      this.searchControl.disable();
    } else {
      this.searchControl.enable();
    }
  }

  private applyFilter(query: string): void {
    const term = query.trim().toLowerCase();
    this.filteredOptions.set(term ? this.options.filter((o) => this.optionLabel(o).toLowerCase().includes(term)) : this.options);
  }

  private syncDisplayFromValue(): void {
    const match = this.options.find((o) => this.optionValue(o) === this.selectedValue);
    const label = match ? this.optionLabel(match) : '';
    this.searchControl.setValue(label, { emitEvent: false });
    this.applyFilter(label);
  }
}
