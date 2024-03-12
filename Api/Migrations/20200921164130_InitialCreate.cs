using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Api.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:exif_orientation", "undefined,normal,flip_horizontal,rotate180,flip_vertical,transpose,rotate90,transverse,rotate270")
                .Annotation("Npgsql:Enum:notification_type", "shot_posted,shot_include_person_tag,shot_liked,commented,comment_include_person_tag,comment_liked,followed")
                .Annotation("Npgsql:Enum:verification_method", "email,sms")
                .Annotation("Npgsql:Enum:verification_purpose", "sign_up,change_email,reset_password")
                .Annotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.CreateSequence(
                name: "BrandHiLoSequence",
                incrementBy: 10);

            migrationBuilder.CreateSequence(
                name: "CommentHiLoSequence",
                incrementBy: 10);

            migrationBuilder.CreateSequence(
                name: "HashTagHiLoSequence",
                incrementBy: 10);

            migrationBuilder.CreateSequence(
                name: "ImageHiLoSequence",
                incrementBy: 10);

            migrationBuilder.CreateSequence(
                name: "ItemTagHiLoSequence",
                incrementBy: 10);

            migrationBuilder.CreateSequence(
                name: "NotificationHiLoSequence",
                incrementBy: 10);

            migrationBuilder.CreateSequence(
                name: "PersonHiLoSequence",
                incrementBy: 10);

            migrationBuilder.CreateSequence(
                name: "ProductHiLoSequence",
                incrementBy: 10);

            migrationBuilder.CreateSequence(
                name: "PushTokenHiLoSequence",
                incrementBy: 10);

            migrationBuilder.CreateSequence(
                name: "ShotHiLoSequence",
                incrementBy: 10);

            migrationBuilder.CreateSequence(
                name: "VerificationHiLoSequence",
                incrementBy: 10);

            migrationBuilder.CreateTable(
                name: "brand",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SequenceHiLo),
                    created_at = table.Column<DateTimeOffset>(nullable: false, defaultValueSql: "current_timestamp"),
                    updated_at = table.Column<DateTimeOffset>(nullable: true),
                    code = table.Column<string>(type: "citext", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_brand", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "hash_tag",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SequenceHiLo),
                    created_at = table.Column<DateTimeOffset>(nullable: false, defaultValueSql: "current_timestamp"),
                    updated_at = table.Column<DateTimeOffset>(nullable: true),
                    tag = table.Column<string>(type: "citext", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hash_tag", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "person",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SequenceHiLo),
                    created_at = table.Column<DateTimeOffset>(nullable: false, defaultValueSql: "current_timestamp"),
                    updated_at = table.Column<DateTimeOffset>(nullable: true),
                    name = table.Column<string>(type: "citext", maxLength: 20, nullable: false),
                    email = table.Column<string>(type: "citext", nullable: true),
                    phone_number = table.Column<string>(nullable: true),
                    hashed_password = table.Column<string>(nullable: false),
                    biography = table.Column<string>(maxLength: 300, nullable: false),
                    is_enabled = table.Column<bool>(nullable: false),
                    last_name_updated_at = table.Column<DateTimeOffset>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_person", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "product",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SequenceHiLo),
                    created_at = table.Column<DateTimeOffset>(nullable: false, defaultValueSql: "current_timestamp"),
                    updated_at = table.Column<DateTimeOffset>(nullable: true),
                    code = table.Column<string>(type: "citext", nullable: false),
                    brand_id = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product", x => x.id);
                    table.ForeignKey(
                        name: "FK_product_brand_brand_id",
                        column: x => x.brand_id,
                        principalTable: "brand",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "follow_person",
                columns: table => new
                {
                    follower_id = table.Column<long>(nullable: false),
                    followed_id = table.Column<long>(nullable: false),
                    created_at = table.Column<DateTimeOffset>(nullable: false, defaultValueSql: "current_timestamp"),
                    updated_at = table.Column<DateTimeOffset>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_follow_person", x => new { x.follower_id, x.followed_id });
                    table.ForeignKey(
                        name: "FK_follow_person_person_followed_id",
                        column: x => x.followed_id,
                        principalTable: "person",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_follow_person_person_follower_id",
                        column: x => x.follower_id,
                        principalTable: "person",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "push_token",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SequenceHiLo),
                    created_at = table.Column<DateTimeOffset>(nullable: false, defaultValueSql: "current_timestamp"),
                    updated_at = table.Column<DateTimeOffset>(nullable: true),
                    token = table.Column<string>(nullable: false),
                    person_id = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_push_token", x => x.id);
                    table.ForeignKey(
                        name: "FK_push_token_person_person_id",
                        column: x => x.person_id,
                        principalTable: "person",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shot",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SequenceHiLo),
                    created_at = table.Column<DateTimeOffset>(nullable: false, defaultValueSql: "current_timestamp"),
                    updated_at = table.Column<DateTimeOffset>(nullable: true),
                    caption = table.Column<string>(maxLength: 300, nullable: false),
                    person_id = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shot", x => x.id);
                    table.ForeignKey(
                        name: "FK_shot_person_person_id",
                        column: x => x.person_id,
                        principalTable: "person",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "verification",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SequenceHiLo),
                    created_at = table.Column<DateTimeOffset>(nullable: false, defaultValueSql: "current_timestamp"),
                    updated_at = table.Column<DateTimeOffset>(nullable: true),
                    app_uuid = table.Column<string>(nullable: false),
                    requester_id = table.Column<long>(nullable: true),
                    purpose = table.Column<int>(nullable: false),
                    method = table.Column<int>(nullable: false),
                    to = table.Column<string>(nullable: false),
                    code = table.Column<string>(nullable: false),
                    message_id = table.Column<string>(nullable: false),
                    re_request_count = table.Column<int>(nullable: false),
                    requested_at = table.Column<DateTimeOffset>(nullable: false, defaultValueSql: "current_timestamp"),
                    verified_at = table.Column<DateTimeOffset>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_verification", x => x.id);
                    table.ForeignKey(
                        name: "FK_verification_person_requester_id",
                        column: x => x.requester_id,
                        principalTable: "person",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "comment",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SequenceHiLo),
                    created_at = table.Column<DateTimeOffset>(nullable: false, defaultValueSql: "current_timestamp"),
                    updated_at = table.Column<DateTimeOffset>(nullable: true),
                    text = table.Column<string>(maxLength: 300, nullable: false),
                    shot_id = table.Column<long>(nullable: false),
                    person_id = table.Column<long>(nullable: false),
                    parent_id = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_comment", x => x.id);
                    table.ForeignKey(
                        name: "FK_comment_comment_parent_id",
                        column: x => x.parent_id,
                        principalTable: "comment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_comment_person_person_id",
                        column: x => x.person_id,
                        principalTable: "person",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_comment_shot_shot_id",
                        column: x => x.shot_id,
                        principalTable: "shot",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "image",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SequenceHiLo),
                    created_at = table.Column<DateTimeOffset>(nullable: false, defaultValueSql: "current_timestamp"),
                    updated_at = table.Column<DateTimeOffset>(nullable: true),
                    bucket = table.Column<string>(nullable: false),
                    key = table.Column<string>(nullable: false),
                    file_name = table.Column<string>(nullable: false),
                    content_type = table.Column<string>(nullable: false),
                    raw_format = table.Column<string>(nullable: false),
                    orientation = table.Column<int>(nullable: false),
                    width = table.Column<int>(nullable: false),
                    height = table.Column<int>(nullable: false),
                    length = table.Column<long>(nullable: false),
                    shot_id = table.Column<long>(nullable: true),
                    profile_person_id = table.Column<long>(nullable: true),
                    closet_background_person_id = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_image", x => x.id);
                    table.ForeignKey(
                        name: "FK_image_person_closet_background_person_id",
                        column: x => x.closet_background_person_id,
                        principalTable: "person",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_image_person_profile_person_id",
                        column: x => x.profile_person_id,
                        principalTable: "person",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_image_shot_shot_id",
                        column: x => x.shot_id,
                        principalTable: "shot",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "like_shot",
                columns: table => new
                {
                    person_id = table.Column<long>(nullable: false),
                    shot_id = table.Column<long>(nullable: false),
                    created_at = table.Column<DateTimeOffset>(nullable: false, defaultValueSql: "current_timestamp"),
                    updated_at = table.Column<DateTimeOffset>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_like_shot", x => new { x.person_id, x.shot_id });
                    table.ForeignKey(
                        name: "FK_like_shot_person_person_id",
                        column: x => x.person_id,
                        principalTable: "person",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_like_shot_shot_shot_id",
                        column: x => x.shot_id,
                        principalTable: "shot",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shot_hash_tag",
                columns: table => new
                {
                    shot_id = table.Column<long>(nullable: false),
                    hash_tag_id = table.Column<long>(nullable: false),
                    created_at = table.Column<DateTimeOffset>(nullable: false, defaultValueSql: "current_timestamp"),
                    updated_at = table.Column<DateTimeOffset>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shot_hash_tag", x => new { x.shot_id, x.hash_tag_id });
                    table.ForeignKey(
                        name: "FK_shot_hash_tag_hash_tag_hash_tag_id",
                        column: x => x.hash_tag_id,
                        principalTable: "hash_tag",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_shot_hash_tag_shot_shot_id",
                        column: x => x.shot_id,
                        principalTable: "shot",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "like_comment",
                columns: table => new
                {
                    person_id = table.Column<long>(nullable: false),
                    comment_id = table.Column<long>(nullable: false),
                    created_at = table.Column<DateTimeOffset>(nullable: false, defaultValueSql: "current_timestamp"),
                    updated_at = table.Column<DateTimeOffset>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_like_comment", x => new { x.person_id, x.comment_id });
                    table.ForeignKey(
                        name: "FK_like_comment_comment_comment_id",
                        column: x => x.comment_id,
                        principalTable: "comment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_like_comment_person_person_id",
                        column: x => x.person_id,
                        principalTable: "person",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notification",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SequenceHiLo),
                    created_at = table.Column<DateTimeOffset>(nullable: false, defaultValueSql: "current_timestamp"),
                    updated_at = table.Column<DateTimeOffset>(nullable: true),
                    type = table.Column<int>(nullable: false),
                    shot_id = table.Column<long>(nullable: true),
                    comment_id = table.Column<long>(nullable: true),
                    producer_id = table.Column<long>(nullable: false),
                    consumer_id = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification", x => x.id);
                    table.ForeignKey(
                        name: "FK_notification_comment_comment_id",
                        column: x => x.comment_id,
                        principalTable: "comment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_notification_person_consumer_id",
                        column: x => x.consumer_id,
                        principalTable: "person",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_notification_person_producer_id",
                        column: x => x.producer_id,
                        principalTable: "person",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_notification_shot_shot_id",
                        column: x => x.shot_id,
                        principalTable: "shot",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "item_tag",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SequenceHiLo),
                    created_at = table.Column<DateTimeOffset>(nullable: false, defaultValueSql: "current_timestamp"),
                    updated_at = table.Column<DateTimeOffset>(nullable: true),
                    x = table.Column<float>(nullable: false),
                    y = table.Column<float>(nullable: false),
                    product_id = table.Column<long>(nullable: false),
                    image_id = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_tag", x => x.id);
                    table.ForeignKey(
                        name: "FK_item_tag_image_image_id",
                        column: x => x.image_id,
                        principalTable: "image",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_item_tag_product_product_id",
                        column: x => x.product_id,
                        principalTable: "product",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_brand_code",
                table: "brand",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_comment_parent_id",
                table: "comment",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "IX_comment_person_id",
                table: "comment",
                column: "person_id");

            migrationBuilder.CreateIndex(
                name: "IX_comment_shot_id",
                table: "comment",
                column: "shot_id");

            migrationBuilder.CreateIndex(
                name: "IX_follow_person_followed_id",
                table: "follow_person",
                column: "followed_id");

            migrationBuilder.CreateIndex(
                name: "IX_hash_tag_tag",
                table: "hash_tag",
                column: "tag",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_image_closet_background_person_id",
                table: "image",
                column: "closet_background_person_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_image_profile_person_id",
                table: "image",
                column: "profile_person_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_image_shot_id",
                table: "image",
                column: "shot_id");

            migrationBuilder.CreateIndex(
                name: "IX_image_bucket_key",
                table: "image",
                columns: new[] { "bucket", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_item_tag_image_id",
                table: "item_tag",
                column: "image_id");

            migrationBuilder.CreateIndex(
                name: "IX_item_tag_product_id",
                table: "item_tag",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_like_comment_comment_id",
                table: "like_comment",
                column: "comment_id");

            migrationBuilder.CreateIndex(
                name: "IX_like_shot_shot_id",
                table: "like_shot",
                column: "shot_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_comment_id",
                table: "notification",
                column: "comment_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_consumer_id",
                table: "notification",
                column: "consumer_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_producer_id",
                table: "notification",
                column: "producer_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_shot_id",
                table: "notification",
                column: "shot_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_type_shot_id_comment_id_consumer_id_producer_id",
                table: "notification",
                columns: new[] { "type", "shot_id", "comment_id", "consumer_id", "producer_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_person_email",
                table: "person",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_person_name",
                table: "person",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_person_phone_number",
                table: "person",
                column: "phone_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_brand_id_code",
                table: "product",
                columns: new[] { "brand_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_push_token_token",
                table: "push_token",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_push_token_person_id_token",
                table: "push_token",
                columns: new[] { "person_id", "token" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shot_person_id",
                table: "shot",
                column: "person_id");

            migrationBuilder.CreateIndex(
                name: "IX_shot_hash_tag_hash_tag_id",
                table: "shot_hash_tag",
                column: "hash_tag_id");

            migrationBuilder.CreateIndex(
                name: "IX_verification_requester_id",
                table: "verification",
                column: "requester_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "follow_person");

            migrationBuilder.DropTable(
                name: "item_tag");

            migrationBuilder.DropTable(
                name: "like_comment");

            migrationBuilder.DropTable(
                name: "like_shot");

            migrationBuilder.DropTable(
                name: "notification");

            migrationBuilder.DropTable(
                name: "push_token");

            migrationBuilder.DropTable(
                name: "shot_hash_tag");

            migrationBuilder.DropTable(
                name: "verification");

            migrationBuilder.DropTable(
                name: "image");

            migrationBuilder.DropTable(
                name: "product");

            migrationBuilder.DropTable(
                name: "comment");

            migrationBuilder.DropTable(
                name: "hash_tag");

            migrationBuilder.DropTable(
                name: "brand");

            migrationBuilder.DropTable(
                name: "shot");

            migrationBuilder.DropTable(
                name: "person");

            migrationBuilder.DropSequence(
                name: "BrandHiLoSequence");

            migrationBuilder.DropSequence(
                name: "CommentHiLoSequence");

            migrationBuilder.DropSequence(
                name: "HashTagHiLoSequence");

            migrationBuilder.DropSequence(
                name: "ImageHiLoSequence");

            migrationBuilder.DropSequence(
                name: "ItemTagHiLoSequence");

            migrationBuilder.DropSequence(
                name: "NotificationHiLoSequence");

            migrationBuilder.DropSequence(
                name: "PersonHiLoSequence");

            migrationBuilder.DropSequence(
                name: "ProductHiLoSequence");

            migrationBuilder.DropSequence(
                name: "PushTokenHiLoSequence");

            migrationBuilder.DropSequence(
                name: "ShotHiLoSequence");

            migrationBuilder.DropSequence(
                name: "VerificationHiLoSequence");
        }
    }
}
