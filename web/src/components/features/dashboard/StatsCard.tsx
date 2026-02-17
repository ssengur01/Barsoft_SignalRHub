import React from 'react';

interface StatsCardProps {
  title: string;
  value: number;
  icon?: React.ReactNode;
  variant?: 'primary' | 'success' | 'warning';
}

export const StatsCard: React.FC<StatsCardProps> = ({ title, value, icon, variant = 'primary' }) => {
  const variantStyles = {
    primary: 'from-blue-500 to-blue-600',
    success: 'from-green-500 to-green-600',
    warning: 'from-yellow-500 to-yellow-600',
  };

  return (
    <div className={`bg-gradient-to-br ${variantStyles[variant]} rounded-lg shadow-lg p-6 text-white`}>
      <div className="flex items-center justify-between">
        <div>
          <p className="text-sm font-medium text-white/80">{title}</p>
          <p className="text-3xl font-bold mt-2">{value.toLocaleString()}</p>
        </div>
        {icon && (
          <div className="text-white/80">
            {icon}
          </div>
        )}
      </div>
    </div>
  );
};
