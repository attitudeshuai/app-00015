using BalconyFarm.Application.DTOs;
using BalconyFarm.Domain.Entities;
using Mapster;

namespace BalconyFarm.Application.Extensions;

public static class MappingConfig
{
    public static void ConfigureMappings()
    {
        TypeAdapterConfig<User, UserDto>.NewConfig()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Username, src => src.Username)
            .Map(dest => dest.Email, src => src.Email)
            .Map(dest => dest.Avatar, src => src.Avatar)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt);

        TypeAdapterConfig<RegisterRequestDto, User>.NewConfig()
            .Map(dest => dest.Username, src => src.Username)
            .Map(dest => dest.Email, src => src.Email)
            .Map(dest => dest.Avatar, src => src.Avatar)
            .Map(dest => dest.PasswordHash, src => "");

        TypeAdapterConfig<CreateCropRequestDto, Crop>.NewConfig()
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.Variety, src => src.Variety)
            .Map(dest => dest.PlantingDate, src => src.PlantingDate)
            .Map(dest => dest.Location, src => src.Location)
            .Map(dest => dest.ContainerType, src => src.ContainerType)
            .Map(dest => dest.Status, src => src.Status)
            .Map(dest => dest.PhotoUrl, src => src.PhotoUrl);

        TypeAdapterConfig<Crop, CropDto>.NewConfig()
            .Map(dest => dest.OwnerUsername, src => src.User != null ? src.User.Username : null);

        TypeAdapterConfig<CreateCropCareTaskRequestDto, CropCareTask>.NewConfig()
            .Map(dest => dest.CropId, src => src.CropId)
            .Map(dest => dest.TaskType, src => src.TaskType)
            .Map(dest => dest.ScheduledDate, src => src.ScheduledDate)
            .Map(dest => dest.Note, src => src.Note)
            .Map(dest => dest.Status, src => Domain.Enums.TaskStatus.Pending);

        TypeAdapterConfig<CropCareTask, CropCareTaskDto>.NewConfig()
            .Map(dest => dest.CropName, src => src.Crop != null ? src.Crop.Name : null);

        TypeAdapterConfig<CreateHarvestRecordRequestDto, HarvestRecord>.NewConfig()
            .Map(dest => dest.CropId, src => src.CropId)
            .Map(dest => dest.HarvestDate, src => src.HarvestDate)
            .Map(dest => dest.Quantity, src => src.Quantity)
            .Map(dest => dest.Unit, src => src.Unit)
            .Map(dest => dest.QualityNote, src => src.QualityNote)
            .Map(dest => dest.PhotoUrl, src => src.PhotoUrl);

        TypeAdapterConfig<HarvestRecord, HarvestRecordDto>.NewConfig()
            .Map(dest => dest.CropName, src => src.Crop != null ? src.Crop.Name : null);

        TypeAdapterConfig<CreatePestRecordRequestDto, PestRecord>.NewConfig()
            .Map(dest => dest.CropId, src => src.CropId)
            .Map(dest => dest.IssueType, src => src.IssueType)
            .Map(dest => dest.Symptoms, src => src.Symptoms)
            .Map(dest => dest.Treatment, src => src.Treatment)
            .Map(dest => dest.DetectedDate, src => src.DetectedDate)
            .Map(dest => dest.Status, src => src.Status);

        TypeAdapterConfig<PestRecord, PestRecordDto>.NewConfig()
            .Map(dest => dest.CropName, src => src.Crop != null ? src.Crop.Name : null);

        TypeAdapterConfig<CreateTreatmentLogRequestDto, TreatmentLog>.NewConfig()
            .Map(dest => dest.Medication, src => src.Medication)
            .Map(dest => dest.Dosage, src => src.Dosage)
            .Map(dest => dest.SymptomChange, src => src.SymptomChange)
            .Map(dest => dest.TreatmentDate, src => src.TreatmentDate)
            .Map(dest => dest.Note, src => src.Note);
    }
}
