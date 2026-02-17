import { useEffect, useState, useRef } from 'react';
import { apiService } from '../../../services/api.service';
import { useEventStore } from '../../../stores/eventStore';
import type { StokHareketDto } from '../../../types/api.types';
import { Card } from '../../ui/Card';
import { Spinner } from '../../ui/Spinner';
import { format } from 'date-fns';

export const RecentMovements = () => {
  const [movements, setMovements] = useState<StokHareketDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const events = useEventStore((state) => state.events);
  const processedEventCount = useRef(0);

  const fetchMovements = async () => {
    try {
      setIsLoading(true);
      const data = await apiService.getRecentStokHareketler(10);
      setMovements(data);
      setError(null);
      console.log('[RecentMovements] Loaded', data.length, 'movements');
    } catch (err) {
      console.error('[RecentMovements] Failed to fetch:', err);
      setError('Failed to load recent movements');
    } finally {
      setIsLoading(false);
    }
  };

  // Initial load
  useEffect(() => {
    fetchMovements();
  }, []);

  // Listen to real-time events and refresh table
  useEffect(() => {
    // Process ALL new events, not just the latest one
    const newEventCount = events.length;
    const unprocessedCount = newEventCount - processedEventCount.current;

    if (unprocessedCount > 0) {
      console.log(`[RecentMovements] Processing ${unprocessedCount} new event(s)...`);

      try {
        // Get all new events (from most recent to least recent)
        const newEvents = events.slice(0, unprocessedCount);

        // Process each new event
        newEvents.reverse().forEach((event) => {
          console.log('[RecentMovements] Processing event:', event.type, event.data);

          // Validate event data
          if (!event || !event.data) {
            console.error('[RecentMovements] Invalid event data:', event);
            return;
          }

          // Add the new item to the top of the list
          if (event.type === 'created') {
            setMovements((prev) => {
              const newMovement = event.data as StokHareketDto;

              // Validate required fields
              if (!newMovement || typeof newMovement.id === 'undefined') {
                console.error('[RecentMovements] Invalid movement data:', newMovement);
                return prev;
              }

              // Check if already exists
              if (prev.some(m => m.id === newMovement.id)) {
                console.log('[RecentMovements] Movement already exists:', newMovement.id);
                return prev;
              }

              console.log('[RecentMovements] Adding new movement to top:', newMovement.id);
              // Add to top and keep only 10
              return [newMovement, ...prev].slice(0, 10);
            });
          } else if (event.type === 'updated') {
            setMovements((prev) =>
              prev.map(m =>
                m.id === event.data.id ? event.data as StokHareketDto : m
              )
            );
          }
        });

        // Update the processed count
        processedEventCount.current = newEventCount;
      } catch (error) {
        console.error('[RecentMovements] Error processing events:', error);
      }
    }
  }, [events]);

  if (isLoading) {
    return (
      <Card>
        <div className="flex items-center justify-center py-8">
          <Spinner size="lg" />
        </div>
      </Card>
    );
  }

  if (error) {
    return (
      <Card>
        <div className="text-center py-8 text-red-600">
          {error}
        </div>
      </Card>
    );
  }

  return (
    <Card>
      <div className="px-6 py-4 border-b border-gray-200">
        <h2 className="text-lg font-semibold text-gray-900">
          Son 10 Stok Hareketi
        </h2>
        <p className="text-sm text-gray-600 mt-1">
          Veritabanındaki en son kayıtlar
        </p>
      </div>

      <div className="overflow-x-auto">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                ID
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Belge Kodu
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Tarih
              </th>
              <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                Miktar
              </th>
              <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                Birim Fiyat
              </th>
              <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                Toplam Tutar
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Açıklama
              </th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {movements.length === 0 ? (
              <tr>
                <td colSpan={7} className="px-6 py-8 text-center text-gray-500">
                  Kayıt bulunamadı
                </td>
              </tr>
            ) : (
              movements.map((movement) => {
                try {
                  return (
                    <tr key={movement.id} className="hover:bg-gray-50">
                      <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                        {movement.id ?? '-'}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                        {movement.belgeKodu ?? '-'}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-600">
                        {movement.belgeTarihi ? format(new Date(movement.belgeTarihi), 'dd.MM.yyyy') : '-'}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-right text-gray-900">
                        {movement.miktar != null ? movement.miktar.toFixed(2) : '-'}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-right text-gray-900">
                        {movement.birimFiyati != null ? `₺${movement.birimFiyati.toFixed(2)}` : '-'}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-right font-medium text-gray-900">
                        {movement.toplamTutar != null ? `₺${movement.toplamTutar.toFixed(2)}` : '-'}
                      </td>
                      <td className="px-6 py-4 text-sm text-gray-600 max-w-xs truncate">
                        {movement.aciklama || '-'}
                      </td>
                    </tr>
                  );
                } catch (error) {
                  console.error('[RecentMovements] Error rendering movement:', movement, error);
                  return null;
                }
              })
            )}
          </tbody>
        </table>
      </div>
    </Card>
  );
};
