import {
  Utensils,
  Car,
  Home,
  HeartPulse,
  Tv,
  ShoppingBag,
  GraduationCap,
  Landmark,
  TrendingUp,
  Tag,
  ShoppingCart,
  Bike,
  Croissant,
  Fuel,
  Ticket,
  CircleParking,
  Wrench,
  Building,
  Building2,
  Zap,
  Droplet,
  Flame,
  Wifi,
  Pill,
  Stethoscope,
  ShieldPlus,
  Dumbbell,
  Play,
  Plane,
  Gamepad2,
  Shirt,
  Laptop,
  Sparkles,
  Lamp,
  PawPrint,
  Paperclip,
  Store,
  Video,
  BookOpen,
  School,
  Receipt,
  FileText,
  Percent,
  Shield,
  Briefcase,
  Coins,
  RotateCcw,
  ArrowDownLeft,
  SlidersHorizontal,
  ArrowLeftRight,
  type LucideIcon,
} from 'lucide-react';

export const iconMap: Record<string, LucideIcon> = {
  // Categorias Principais
  utensils: Utensils,
  car: Car,
  home: Home,
  'heart-pulse': HeartPulse,
  tv: Tv,
  'shopping-bag': ShoppingBag,
  'graduation-cap': GraduationCap,
  landmark: Landmark,
  'trending-up': TrendingUp,
  tag: Tag,

  // Alimentação
  'shopping-cart': ShoppingCart,
  bike: Bike,
  croissant: Croissant,

  // Transporte
  fuel: Fuel,
  ticket: Ticket,
  'circle-parking': CircleParking,
  wrench: Wrench,

  // Moradia
  building: Building,
  'building-2': Building2,
  zap: Zap,
  droplet: Droplet,
  flame: Flame,
  wifi: Wifi,

  // Saúde
  pill: Pill,
  stethoscope: Stethoscope,
  'shield-plus': ShieldPlus,
  dumbbell: Dumbbell,

  // Lazer
  play: Play,
  plane: Plane,
  gamepad: Gamepad2,
  'gamepad-2': Gamepad2,

  // Compras
  shirt: Shirt,
  laptop: Laptop,
  sparkles: Sparkles,
  lamp: Lamp,
  'paw-print': PawPrint,
  paperclip: Paperclip,
  store: Store,

  // Educação
  video: Video,
  'book-open': BookOpen,
  school: School,

  // Finanças
  receipt: Receipt,
  'file-text': FileText,
  percent: Percent,
  shield: Shield,

  // Receitas
  briefcase: Briefcase,
  coins: Coins,
  'rotate-ccw': RotateCcw,
  'arrow-down-left': ArrowDownLeft,

  // Outros
  sliders: SlidersHorizontal,
  'sliders-horizontal': SlidersHorizontal,
  'arrow-left-right': ArrowLeftRight,
};

export const getCategoryIcon = (iconKey?: string): LucideIcon => {
  if (!iconKey) return Tag;
  return iconMap[iconKey] || Tag;
};
