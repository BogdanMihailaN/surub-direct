import { OrderProcessing } from './order-processing.model';
export interface Shipment extends OrderProcessing { reference: string | null; }
