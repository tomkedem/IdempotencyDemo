import { Component, OnInit, signal } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormControl } from '@angular/forms';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatOptionModule } from '@angular/material/core';
import { CommonModule } from '@angular/common';
import { ShipmentService } from '../../services/shipment.service';
import { CreateDeliveryRequest } from '../../models/shipment.model';

@Component({
  selector: 'app-create-shipment',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatSelectModule,
    MatOptionModule,
    MatSnackBarModule
  ],
  templateUrl: './create-shipment.component.html',
  styleUrls: ['./create-shipment.component.scss']
})
export class CreateShipmentComponent implements OnInit {
  shipmentForm!: FormGroup; // Use definite assignment assertion
  isLoading = signal(false);
  lastResponse = signal<any>(null);

  // Status options for the dropdown
  statuses = [
    { id: 2, name: 'נמסר', name_en: 'Delivered' },
    { id: 3, name: 'נכשל', name_en: 'Failed' },
    { id: 4, name: 'נמסר חלקית', name_en: 'Partial' }
  ];

  constructor(
    private fb: FormBuilder,
    private shipmentService: ShipmentService,
    private snackBar: MatSnackBar
  ) {
    this.initializeForm();
  }

  ngOnInit(): void {}

  private initializeForm(): void {
    this.shipmentForm = this.fb.group({
      barcode: ['', [Validators.required, Validators.pattern(/^[A-Za-z0-9]{13}$/)]],
      employeeId: ['', [Validators.required, Validators.minLength(2)]],
      locationLat: ['', [Validators.pattern(/^-?\d+\.?\d*$/)]],
      locationLng: ['', [Validators.pattern(/^-?\d+\.?\d*$/)]],
      recipientName: ['', [Validators.minLength(2)]],
      statusId: [null, [Validators.required]],
      notes: ['']
    });
  }

  async onSubmit(): Promise<void> {
    if (this.shipmentForm.valid && !this.isLoading()) {
      this.isLoading.set(true);
      
      try {
        const formValue = this.shipmentForm.value;
        const deliveryRequest: CreateDeliveryRequest = {
          barcode: formValue.barcode,
          employeeId: formValue.employeeId,
          locationLat: formValue.locationLat ? parseFloat(formValue.locationLat) : undefined,
          locationLng: formValue.locationLng ? parseFloat(formValue.locationLng) : undefined,
          recipientName: formValue.recipientName || undefined,
          statusId: formValue.statusId,
          notes: formValue.notes || undefined
        };

        const response = await this.shipmentService.createDelivery(deliveryRequest);
        this.lastResponse.set(response);
        
        this.snackBar.open('מסירת משלוח נרשמה בהצלחה! ✅', 'סגור', {
          duration: 3000,
          horizontalPosition: 'center',
          verticalPosition: 'top'
        });
        
        this.shipmentForm.reset();
        this.initializeForm(); // Reset to default values
      } catch (error) {
        console.error('Error creating delivery:', error);
        this.snackBar.open('שגיאה ברישום מסירת משלוח ❌', 'סגור', {
          duration: 5000,
          horizontalPosition: 'center',
          verticalPosition: 'top'
        });
      } finally {
        this.isLoading.set(false);
      }
    }
  }

  async testConnection(): Promise<void> {
    this.isLoading.set(true);
    
    try {
      const result = await this.shipmentService.testConnection();
      
      if (result.success) {
        this.snackBar.open(`✅ ${result.message}`, 'סגור', {
          duration: 4000,
          horizontalPosition: 'center',
          verticalPosition: 'top'
        });
        this.lastResponse.set({ 
          connectionTest: true, 
          success: true, 
          endpoint: result.endpoint,
          message: result.message 
        });
      } else {
        this.snackBar.open(`❌ ${result.message}`, 'סגור', {
          duration: 6000,
          horizontalPosition: 'center',
          verticalPosition: 'top'
        });
        this.lastResponse.set({ 
          connectionTest: true, 
          success: false, 
          endpoint: result.endpoint,
          error: result.message 
        });
      }
    } catch (error) {
      console.error('Connection test failed:', error);
      this.snackBar.open('❌ בדיקת חיבור נכשלה', 'סגור', {
        duration: 5000,
        horizontalPosition: 'center',
        verticalPosition: 'top'
      });
    } finally {
      this.isLoading.set(false);
    }
  }

  onReset(): void {
    this.shipmentForm.reset();
    this.lastResponse.set(null);
  }

  // Test idempotency by sending the same request multiple times
  async testIdempotency(): Promise<void> {
    if (this.shipmentForm.valid) {
      const request = {
        barcode: this.shipmentForm.value.barcode,
        employeeId: this.shipmentForm.value.employeeId,
        locationLat: Number(this.shipmentForm.value.locationLat),
        locationLng: Number(this.shipmentForm.value.locationLng),
        recipientName: this.shipmentForm.value.recipientName,
        statusId: this.shipmentForm.value.statusId,
        notes: this.shipmentForm.value.notes || undefined
      };

      // Send the same request 3 times to test idempotency
      for (let i = 0; i < 3; i++) {
        setTimeout(async () => {
          try {
            const response = await this.shipmentService.createDelivery(request);
            console.log(`Request ${i + 1} response:`, response);
          } catch (error: any) {
            console.log(`Request ${i + 1} error:`, error.message);
          }
        }, i * 500);
      }

      this.snackBar.open('שולח 3 בקשות זהות לבדיקת אידמפוטנטיות...', 'סגור', {
        duration: 3000
      });
    }
  }
}
