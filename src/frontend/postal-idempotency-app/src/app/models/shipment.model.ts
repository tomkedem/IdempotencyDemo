export interface Shipment {
  // Legacy shipment fields (optional for backward compatibility)
  id: string;
  customerName?: string;
  address?: string;
  weight?: number;
  price?: number;
  status: number;
  statusName?: string;
  statusNameHe?: string;
  createdAt: Date;
  updated_at?: Date;
  notes?: string;
  barcode: string;
  employeeId: string;
  deliveryDate?: Date;
  locationLat?: number;
  locationLng?: number;
  recipientName?: string;
  deliveryStatus?: number; // 1=delivered, 2=failed, 3=partial
}

export enum ShipmentStatus {
  Created = 1,
  InTransit = 2,
  Delivered = 3,
  Cancelled = 4,
  Failed = 5
}

export interface CreateDeliveryRequest {
  barcode: string;
  employeeId?: string;
  locationLat?: number;
  locationLng?: number;
  recipientName?: string;
  statusId: number;
  notes?: string;
}

export interface CreateShipmentRequest {
  barcode: string;
  customerName?: string;
  address?: string;
  weight: number;
  price: number;
  notes?: string;
}

export interface UpdateStatusRequest {
  status: ShipmentStatus;
}

export interface ApiResponse<T> {
  data?: T;
  error?: string;
  message?: string;
}

export interface IdempotencyDemoResponse<T> {
  success: boolean;
  data?: T;
  message?: string;
  executionTimeMs: number;
  wasIdempotentHit: boolean;
  idempotencyKey?: string;
  timestamp: string;
}
