import { useState } from 'react';
import type { StokHareketEvent } from '../../../types/api.types';
import { format } from 'date-fns';
import { ChevronDown, ChevronUp } from 'lucide-react';

interface EventItemProps {
  event: StokHareketEvent;
}

export const EventItem: React.FC<EventItemProps> = ({ event }) => {
  const [isExpanded, setIsExpanded] = useState(false);

  const typeConfig = {
    created: { label: 'CREATED', color: 'bg-green-100 text-green-800 border-green-300' },
    updated: { label: 'UPDATED', color: 'bg-yellow-100 text-yellow-800 border-yellow-300' },
  };

  const config = typeConfig[event.type];

  return (
    <div className={`border-l-4 ${config.color} bg-white rounded-r-lg shadow-sm p-4 mb-3`}>
      <div className="flex items-start justify-between">
        <div className="flex-1">
          <div className="flex items-center gap-2 mb-2">
            <span className={`text-xs font-bold px-2 py-1 rounded ${config.color}`}>
              {config.label}
            </span>
            <span className="text-xs text-gray-500">
              {format(event.timestamp, 'HH:mm:ss.SSS')}
            </span>
            <span className="text-xs font-mono text-gray-700">
              ID: {event.data.id}
            </span>
          </div>
          <div className="text-sm text-gray-800">
            <span className="font-medium">{event.data.belgeKodu}</span>
            {' • '}
            <span>{event.data.aciklama}</span>
            {' • '}
            <span className="font-semibold">{event.data.toplamTutar.toFixed(2)} ₺</span>
          </div>
        </div>
        <button
          onClick={() => setIsExpanded(!isExpanded)}
          className="text-gray-500 hover:text-gray-700"
        >
          {isExpanded ? <ChevronUp className="w-5 h-5" /> : <ChevronDown className="w-5 h-5" />}
        </button>
      </div>

      {isExpanded && (
        <div className="mt-3 pt-3 border-t border-gray-200">
          <pre className="text-xs bg-gray-50 p-3 rounded overflow-x-auto">
            {JSON.stringify(event.data, null, 2)}
          </pre>
        </div>
      )}
    </div>
  );
};
