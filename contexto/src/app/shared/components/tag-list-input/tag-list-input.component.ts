import { Component, forwardRef, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, NG_VALUE_ACCESSOR, ControlValueAccessor } from '@angular/forms';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzTagModule } from 'ng-zorro-antd/tag';

/**
 * Campo de lista de textos (equivalente a List<string> nos DTOs, ex.
 * anuncios, desobrigacoes, apoios...). Digita, aperta "+"/Enter,
 * vira um chip removível. Implementa ControlValueAccessor para
 * ser usado como `formControlName` normalmente.
 */
@Component({
  selector: 'app-tag-list-input',
  standalone: true,
  imports: [CommonModule, FormsModule, NzInputModule, NzButtonModule, NzIconModule, NzTagModule],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => TagListInputComponent),
      multi: true,
    },
  ],
  template: `
    <div class="tag-list">
      @for (item of value; track $index) {
        <nz-tag class="tag-list__chip" [nzColor]="chipColor" (nzOnClose)="remove($index)" [nzMode]="'closeable'">
          {{ item }}
        </nz-tag>
      }
    </div>

    <div class="tag-list__entry">
      <input
        nz-input
        [placeholder]="placeholder"
        [(ngModel)]="draft"
        [disabled]="disabled"
        (keydown.enter)="add(); $event.preventDefault()"
      />
      <button nz-button nzType="default" type="button" (click)="add()" [disabled]="disabled || !draft.trim()">
        <span nz-icon nzType="plus"></span>
      </button>
    </div>
  `,
  styles: [
    `
      :host {
        display: block;
      }
      .tag-list {
        display: flex;
        flex-wrap: wrap;
        gap: 6px;
        margin-bottom: 8px;
        &:empty {
          margin-bottom: 0;
        }
      }
      .tag-list__chip {
        font-size: 13px;
        padding: 4px 8px;
        border-radius: 8px;
      }
      .tag-list__entry {
        display: flex;
        gap: 8px;
      }
      .tag-list__entry input {
        flex: 1;
      }
    `,
  ],
})
export class TagListInputComponent implements ControlValueAccessor {
  @Input() placeholder = 'Adicionar item...';
  @Input() chipColor: string | undefined = undefined;

  value: string[] = [];
  draft = '';
  disabled = false;

  private onChange: (val: string[]) => void = () => {};
  private onTouched: () => void = () => {};

  writeValue(val: string[] | null): void {
    this.value = val ?? [];
  }
  registerOnChange(fn: (val: string[]) => void): void {
    this.onChange = fn;
  }
  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }
  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }

  add(): void {
    const text = this.draft.trim();
    if (!text) return;
    this.value = [...this.value, text];
    this.draft = '';
    this.onChange(this.value);
    this.onTouched();
  }

  remove(index: number): void {
    this.value = this.value.filter((_, i) => i !== index);
    this.onChange(this.value);
    this.onTouched();
  }
}
