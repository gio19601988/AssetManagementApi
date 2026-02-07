// src/components/orders/tabs/OverviewTab.tsx
/**
 * მიმოხილვის ტაბი
 * აჩვენებს შეკვეთის ძირითად ინფორმაციას: სტატუსი, ტიპი, პრიორიტეტი, მომხმარებელი, დეპარტამენტი, თანხა, თარიღები, აღწერა
 */

import { Badge } from "@/components/ui/badge";
import { Card, CardContent } from "@/components/ui/card";
//import type { Order } from "./order";

interface OverviewTabProps {
  order: Order;
}

interface Order {
  id: number;
    orderNumber: string;
    title: string;
    description?: string;
    priority: string;
    estimatedAmount?: number;
    currency: string;
    requestedDate: string;
    requiredByDate?: string;
    approvedDate?: string;
    completedDate?: string;
    createdAt: string;
    updatedAt: string;
  
    statusId: number;
    statusCode: string;
    statusName: string;
    statusNameKa: string;
  
    orderTypeId?: number;
    orderTypeName?: string;
    orderTypeNameKa?: string;
  
    requesterId: number;
    requesterName: string;
  
    departmentId?: number;
    departmentName?: string;
  }

export default function OverviewTab({ order }: OverviewTabProps) {
  return (
    <Card>
      <CardContent className="grid gap-6 md:grid-cols-2 pt-6">
        {/* სტატუსი */}
        <div>
          <p className="text-sm text-muted-foreground">სტატუსი</p>
          <Badge variant="outline" className="mt-1 text-lg">
            {order.statusNameKa}
          </Badge>
        </div>

        {/* ტიპი */}
        <div>
          <p className="text-sm text-muted-foreground">ტიპი</p>
          <p className="font-medium">
            {order.orderTypeNameKa || "არ არის მითითებული"}
          </p>
        </div>

        {/* პრიორიტეტი */}
        <div>
          <p className="text-sm text-muted-foreground">პრიორიტეტი</p>
          <Badge
            variant={
              order.priority === "high"
                ? "destructive"
                : order.priority === "medium"
                ? "default"
                : "secondary"
            }
          >
            {order.priority === "high"
              ? "მაღალი"
              : order.priority === "medium"
              ? "საშუალო"
              : "დაბალი"}
          </Badge>
        </div>

        {/* მომხმარებელი */}
        <div>
          <p className="text-sm text-muted-foreground">მოთხოვნა</p>
          <p className="font-medium">{order.requesterName}</p>
        </div>

        {/* დეპარტამენტი */}
        <div>
          <p className="text-sm text-muted-foreground">დეპარტამენტი</p>
          <p className="font-medium">{order.departmentName || "—"}</p>
        </div>

        {/* ბიუჯეტი */}
        <div>
          <p className="text-sm text-muted-foreground">ბიუჯეტი</p>
          <p className="font-medium">
            {order.estimatedAmount
              ? `${order.estimatedAmount} ${order.currency}`
              : "არ არის მითითებული"}
          </p>
        </div>

        {/* მოთხოვნის თარიღი */}
        <div>
          <p className="text-sm text-muted-foreground">მოთხოვნის თარიღი</p>
          <p className="font-medium">
            {new Date(order.requestedDate).toLocaleDateString("ka-GE")}
          </p>
        </div>

        {/* საჭიროების თარიღი */}
        <div>
          <p className="text-sm text-muted-foreground">საჭიროების თარიღი</p>
          <p className="font-medium">
            {order.requiredByDate
              ? new Date(order.requiredByDate).toLocaleDateString("ka-GE")
              : "—"}
          </p>
        </div>

        {/* აღწერა (თუ არსებობს) */}
        {order.description && (
          <div className="md:col-span-2">
            <p className="text-sm text-muted-foreground">აღწერა</p>
            <p className="font-medium mt-1">{order.description}</p>
          </div>
        )}
      </CardContent>
    </Card>
  );
}