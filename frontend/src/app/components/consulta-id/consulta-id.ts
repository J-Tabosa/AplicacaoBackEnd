import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-consulta-id',
  imports: [FormsModule],
  template: ` <form class="consulta-id" #form="ngForm" (ngSubmit)="consultar.emit(id!)">
    <label for="consulta-id">Consultar por ID</label>
    <input
      id="consulta-id"
      name="id"
      type="number"
      [(ngModel)]="id"
      min="1"
      step="1"
      required
      placeholder="ID do registro"
    />
    <button type="submit" [disabled]="form.invalid || !idValido() || carregando">Consultar</button>
    <button
      type="button"
      class="secundario"
      (click)="id = null; limpar.emit()"
      [disabled]="carregando"
    >
      Listar todos
    </button>
  </form>`,
})
export class ConsultaIdComponent {
  @Input() carregando = false;
  @Output() consultar = new EventEmitter<number>();
  @Output() limpar = new EventEmitter<void>();
  id: number | null = null;
  idValido() {
    return Number.isInteger(this.id) && this.id! > 0;
  }
}
