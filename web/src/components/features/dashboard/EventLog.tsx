import React, { useEffect, useRef } from 'react';
import { Card } from '../../ui/Card';
import { EventItem } from './EventItem';
import { useEventStore } from '../../../stores/eventStore';

export const EventLog: React.FC = () => {
  const events = useEventStore((state) => state.events);
  const logRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    // Auto-scroll to top when new event arrives
    if (logRef.current) {
      logRef.current.scrollTop = 0;
    }
  }, [events]);

  return (
    <Card title="Real-time Event Log">
      <div ref={logRef} className="h-[500px] overflow-y-auto pr-2">
        {events.length === 0 ? (
          <div className="flex items-center justify-center h-full text-gray-500">
            <p className="text-center">
              No events yet.<br />
              <span className="text-sm">Events will appear here in real-time.</span>
            </p>
          </div>
        ) : (
          events.map((event, index) => (
            <EventItem key={`${event.data.id}-${index}`} event={event} />
          ))
        )}
      </div>
    </Card>
  );
};
