using System.Collections.Generic;
using System.Diagnostics;
using AspireApp1.Server.Repositories;
using AspireApp1.Server.DTO;
using AspireApp1.Server.Models;

namespace AspireApp1.Server.Services
{
    public class DessertService
    {
        private readonly IDessertRepository _repo;

        public DessertService(IDessertRepository repo)
        {
            _repo = repo;
        }

        public PagedResultDto<DessertSummaryDto> GetPaged(int page, int pageSize, string? search=null)
        {
            List<Dessert> all = _repo.GetAll(search);
            int total = all.Count;

            List<DessertSummaryDto> items = all
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(DessertMapper.ToSummary)
                .ToList();

            return new PagedResultDto<DessertSummaryDto>
            {
                Total = total,
                Page = page,
                PageSize = pageSize,
                Items = items
            };
        }

        public DessertDetailDto? GetById(int id)
        {
            Dessert? dessert = _repo.GetById(id);
            if (dessert == null) return null;
            return DessertMapper.ToDetail(dessert);
        }

        public DessertDetailDto Add(DessertDetailDto dto)
        {
            Dessert model = DessertMapper.ToModel(dto);
            Dessert created = _repo.Add(model);
            return DessertMapper.ToDetail(created);
        }

        public DessertDetailDto? Update(int id, DessertDetailDto dto)
        {
            Debug.WriteLine("Service console.");
            Dessert model = DessertMapper.ToModel(dto);
            Dessert? updated = _repo.Update(id, model);
            if (updated == null) return null;
            return DessertMapper.ToDetail(updated);
        }

        public bool Delete(int id)
        {
            return _repo.Delete(id);
        }
    }
}
