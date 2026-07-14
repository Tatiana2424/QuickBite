import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useAuth } from "../auth/AuthContext";
import { EmptyState, ErrorState, LoadingState } from "../components/AsyncState";
import type { CourierDeliveryStatus, Delivery } from "../models";
import { getMyCourierDeliveries, updateCourierDeliveryStatus } from "../services/quickbiteService";

export function CourierDeliveriesPage() {
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const canDeliver = user?.roles.some((role) => role === "Courier" || role === "PlatformAdmin");
  const deliveriesQuery = useQuery({
    queryKey: ["courier-deliveries"],
    queryFn: getMyCourierDeliveries,
    enabled: canDeliver,
    refetchInterval: 5_000
  });
  const statusMutation = useMutation({
    mutationFn: ({ deliveryId, status }: { deliveryId: string; status: CourierDeliveryStatus }) =>
      updateCourierDeliveryStatus(deliveryId, status),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["courier-deliveries"] });
    }
  });

  if (!canDeliver) {
    return <ErrorState error={new Error("You need a Courier role to view delivery operations.")} />;
  }

  return (
    <section className="stack">
      <div className="page-heading">
        <p className="eyebrow">Courier</p>
        <h2>Assigned deliveries</h2>
        <p className="muted">Review drop-off addresses and move each delivery from accepted to picked up to delivered.</p>
      </div>
      {deliveriesQuery.isLoading && <LoadingState label="Loading deliveries..." />}
      {deliveriesQuery.isError && <ErrorState error={deliveriesQuery.error} action={<button type="button" onClick={() => void deliveriesQuery.refetch()}>Retry</button>} />}
      {deliveriesQuery.isSuccess && deliveriesQuery.data.length === 0 && (
        <EmptyState title="No assigned deliveries">New deliveries will appear here after customer payments succeed.</EmptyState>
      )}
      <div className="delivery-ops-grid">
        {deliveriesQuery.data?.map((delivery) => (
          <DeliveryOpsCard
            key={delivery.id}
            delivery={delivery}
            isUpdating={statusMutation.isPending}
            onUpdate={(status) => statusMutation.mutate({ deliveryId: delivery.id, status })}
          />
        ))}
      </div>
    </section>
  );
}

function DeliveryOpsCard({
  delivery,
  isUpdating,
  onUpdate
}: {
  delivery: Delivery;
  isUpdating: boolean;
  onUpdate: (status: CourierDeliveryStatus) => void;
}) {
  return (
    <article className="panel lifecycle-card">
      <div className="order-card__header">
        <strong>Order {delivery.orderId.slice(0, 8)}</strong>
        <span className="status-pill">{delivery.status}</span>
      </div>
      <address className="delivery-address">
        <strong>{delivery.address.line1}</strong>
        {delivery.address.line2 && <span>{delivery.address.line2}</span>}
        <span>{delivery.address.city}, {delivery.address.state} {delivery.address.postalCode}</span>
        <span>{delivery.address.country}</span>
      </address>
      <div className="status-actions" aria-label={`Update delivery ${delivery.id}`}>
        <button type="button" className="button-secondary" disabled={isUpdating || delivery.status !== "Assigned"} onClick={() => onUpdate("Accepted")}>
          Accept
        </button>
        <button type="button" className="button-secondary" disabled={isUpdating || delivery.status === "PickedUp" || delivery.status === "Delivered"} onClick={() => onUpdate("PickedUp")}>
          Picked up
        </button>
        <button type="button" disabled={isUpdating || delivery.status === "Delivered"} onClick={() => onUpdate("Delivered")}>
          Delivered
        </button>
      </div>
    </article>
  );
}
