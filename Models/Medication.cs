using System;

namespace MedicalSystem.Models
{
    public class Medication
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public int Quantity { get; set; }
        public int MinThreshold { get; set; }
        public DateTime ExpirationDate { get; set; }
        public string Status { get; set; } = "Disponible";

        // Propriétés calculées
        public bool IsLowStock => Quantity < MinThreshold;
        public bool IsExpiringSoon => (ExpirationDate - DateTime.Now).TotalDays < 30;
        public bool IsExpired => (ExpirationDate - DateTime.Now).TotalDays < 0;
        public int DaysUntilExpiration => (int)(ExpirationDate - DateTime.Now).TotalDays;

        // NOUVELLES MÉTHODES : Vérifications de disponibilité
        /// <summary>
        /// Vérifie si une certaine quantité est disponible
        /// </summary>
        public bool IsQuantityAvailable(int requestedQuantity)
        {
            return Quantity >= requestedQuantity;
        }

        /// <summary>
        /// Vérifie la disponibilité et retourne un message détaillé
        /// </summary>
        public (bool available, string message) CheckAvailability(int requestedQuantity)
        {
            if (Quantity <= 0)
                return (false, $"❌ ÉPUISÉ (Stock: 0)");

            if (Quantity < requestedQuantity)
                return (false, $"❌ STOCK INSUFFISANT (Demande: {requestedQuantity}, Disponible: {Quantity})");

            if (Quantity < MinThreshold)
                return (true, $"⚠️ STOCK FAIBLE (Demande: {requestedQuantity}, Disponible: {Quantity}, Seuil: {MinThreshold})");

            if (IsExpiringSoon)
                return (true, $"⏰ DISPONIBLE (Demande: {requestedQuantity}, Disponible: {Quantity}, Expire dans {DaysUntilExpiration} jours)");

            return (true, $"✅ DISPONIBLE (Demande: {requestedQuantity}, Disponible: {Quantity}, Seuil: {MinThreshold})");
        }

        // NOUVELLE MÉTHODE : Obtenir le statut complet du stock
        public string GetStockStatus()
        {
            if (Quantity <= 0)
                return "ÉPUISÉ";

            if (Quantity < MinThreshold)
                return $"STOCK FAIBLE (Disponible: {Quantity}, Seuil alerte: {MinThreshold})";

            return $"DISPONIBLE (Stock: {Quantity}, Seuil alerte: {MinThreshold})";
        }

        // NOUVELLE PROPRIÉTÉ : Statut détaillé avec emojis
        public string DetailedStatus
        {
            get
            {
                if (Quantity <= 0)
                    return $"❌ Épuisé (0 unités)";

                if (Quantity < MinThreshold)
                    return $"⚠️ Stock faible ({Quantity}/{MinThreshold})";

                if (IsExpiringSoon)
                    return $"⏰ Disponible mais expire bientôt ({Quantity} unités)";

                return $"✅ Disponible ({Quantity} unités, seuil: {MinThreshold})";
            }
        }

        // NOUVELLE MÉTHODE : Résumé complet pour l'affichage
        public string GetFullSummary()
        {
            return $"{Name} ({Code})\n" +
                   $"• Stock: {Quantity} unités\n" +
                   $"• Seuil d'alerte: {MinThreshold} unités\n" +
                   $"• Statut: {DetailedStatus}\n" +
                   $"• Expiration: {ExpirationDate:dd/MM/yyyy} ({DaysUntilExpiration} jours restants)";
        }

        // NOUVELLE MÉTHODE : Vérifier si on peut déduire une quantité
        public (bool canDeduct, string message) CanDeductQuantity(int amount)
        {
            if (amount <= 0)
                return (false, "Quantité invalide");

            if (Quantity < amount)
                return (false, $"Stock insuffisant. Demande: {amount}, Disponible: {Quantity}");

            if (Quantity - amount < MinThreshold)
                return (true, $"ATTENTION: Après déduction, le stock sera en alerte. Nouveau stock: {Quantity - amount}, Seuil: {MinThreshold}");

            return (true, "OK");
        }

        // Méthode pour déduire la quantité (AMÉLIORÉE)
        public (bool success, string message) DeductQuantity(int amount)
        {
            if (amount <= 0)
                return (false, "Quantité invalide");

            if (Quantity < amount)
                return (false, $"Stock insuffisant. Demande: {amount}, Disponible: {Quantity}");

            Quantity -= amount;
            UpdateStatus();

            string message = $"Déduction réussie: -{amount} unités\n";
            message += $"Nouveau stock: {Quantity} unités\n";

            if (Quantity < MinThreshold)
                message += $"⚠️ ALERTE: Stock maintenant en dessous du seuil ({Quantity}/{MinThreshold})";

            return (true, message);
        }

        // Méthode pour ajouter de la quantité (AMÉLIORÉE)
        public (bool success, string message) AddQuantity(int amount)
        {
            if (amount <= 0)
                return (false, "Quantité invalide");

            int oldQuantity = Quantity;
            Quantity += amount;
            UpdateStatus();

            string message = $"Ajout réussi: +{amount} unités\n";
            message += $"Ancien stock: {oldQuantity}, Nouveau stock: {Quantity}\n";

            if (oldQuantity < MinThreshold && Quantity >= MinThreshold)
                message += $"✅ Stock maintenant au-dessus du seuil ({Quantity}/{MinThreshold})";

            return (true, message);
        }

        // NOUVELLE MÉTHODE : Mettre à jour le seuil
        public (bool success, string message) UpdateThreshold(int newThreshold)
        {
            if (newThreshold < 0)
                return (false, "Seuil invalide");

            int oldThreshold = MinThreshold;
            MinThreshold = newThreshold;

            // Mettre à jour le statut avec le nouveau seuil
            UpdateStatus();

            string message = $"Seuil mis à jour: {oldThreshold} → {newThreshold}\n";

            if (Quantity < newThreshold)
                message += $"⚠️ ALERTE: Stock maintenant en dessous du nouveau seuil ({Quantity}/{newThreshold})";
            else if (Quantity >= oldThreshold && Quantity < newThreshold)
                message += $"⚠️ ATTENTION: Avec l'ancien seuil le stock était OK, mais avec le nouveau seuil il est en alerte";
            else
                message += $"✅ Stock OK par rapport au nouveau seuil ({Quantity}/{newThreshold})";

            return (true, message);
        }

        // NOUVELLE MÉTHODE : Obtenir les recommandations de réapprovisionnement
        public (int recommendedOrder, string reason) GetReplenishmentRecommendation()
        {
            if (Quantity <= 0)
                return (MinThreshold * 2, "Stock épuisé - commande urgente nécessaire");

            if (Quantity < MinThreshold)
                return (MinThreshold * 2 - Quantity, "Stock en dessous du seuil d'alerte");

            if (IsExpiringSoon)
                return (MinThreshold, "Produit expire bientôt - commander nouvelle batch");

            // Si tout va bien, recommander de maintenir le stock au double du seuil
            int targetStock = MinThreshold * 2;
            if (Quantity < targetStock)
                return (targetStock - Quantity, "Maintenir le stock optimal");

            return (0, "Stock suffisant - pas besoin de commander");
        }

        // Mettre à jour le statut basé sur la quantité (AMÉLIORÉE)
        private void UpdateStatus()
        {
            if (IsExpired)
                Status = "Expiré";
            else if (Quantity <= 0)
                Status = "Épuisé";
            else if (Quantity < MinThreshold)
                Status = "Faible stock";
            else if (IsExpiringSoon)
                Status = "Expire bientôt";
            else
                Status = "Disponible";
        }

        // NOUVELLE MÉTHODE : Pour l'affichage dans les interfaces
        public override string ToString()
        {
            return $"{Name} ({Code}) - {Quantity} unités - {DetailedStatus}";
        }
    }
}